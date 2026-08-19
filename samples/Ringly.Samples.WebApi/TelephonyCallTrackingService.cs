using System.Collections.Concurrent;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi;

// Writes/updates TelephonyCall rows as real calls progress through RideHailingCallRouter's
// lifecycle (Initiated/Answered/Ended) — a separate service rather than a second subscriber to
// raw ARI Stasis events, since RideHailingCallRouter is the only place that correctly classifies
// caller-leg-vs-callee-leg (see ICallLifecycleEventSource's own comment).
public class TelephonyCallTrackingService : BackgroundService
{
    private readonly ICallLifecycleEventSource callLifecycleEventSource;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<TelephonyCallTrackingService> logger;

    // CallId (the caller's own Asterisk channel ID) -> the TelephonyCall row this service created
    // for it. Necessary regardless of any DB lookup, since evt.CallId is a channel ID string, not
    // the row's own Guid.
    private readonly ConcurrentDictionary<string, Guid> telephonyCallIdByCallId = new();

    public TelephonyCallTrackingService(
        ICallLifecycleEventSource callLifecycleEventSource,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TelephonyCallTrackingService> logger)
    {
        this.callLifecycleEventSource = callLifecycleEventSource;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IDisposable subscription = this.callLifecycleEventSource.StreamCallLifecycleEvents()
            .Subscribe(callLifecycleEvent => this.OnCallLifecycleEvent(callLifecycleEvent));

        stoppingToken.Register(() => subscription.Dispose());

        return Task.CompletedTask;
    }

    private async void OnCallLifecycleEvent(CallLifecycleEvent callLifecycleEvent)
    {
        try
        {
            await this.HandleCallLifecycleEventAsync(callLifecycleEvent);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to record TelephonyCall history for call {CallId}, phase {Phase}",
                callLifecycleEvent.CallId,
                callLifecycleEvent.Phase);
        }
    }

    private async Task HandleCallLifecycleEventAsync(CallLifecycleEvent callLifecycleEvent)
    {
        // ITelephonyCallService/ITelephonyIdentityService are Scoped (they ultimately depend on
        // IStorageBroker, a DbContext) — this service is a Singleton hosted service, so a scope is
        // created per event rather than holding either dependency directly, the standard pattern
        // for accessing Scoped services from a Singleton.
        using IServiceScope scope = this.serviceScopeFactory.CreateScope();

        ITelephonyCallService telephonyCallService =
            scope.ServiceProvider.GetRequiredService<ITelephonyCallService>();

        switch (callLifecycleEvent.Phase)
        {
            case CallLifecyclePhase.Initiated:
                await this.HandleInitiatedAsync(callLifecycleEvent, scope, telephonyCallService);
                break;

            case CallLifecyclePhase.Answered:
                await HandleAnsweredAsync(callLifecycleEvent, telephonyCallService, this.telephonyCallIdByCallId);
                break;

            case CallLifecyclePhase.Ended:
                await HandleEndedAsync(callLifecycleEvent, telephonyCallService, this.telephonyCallIdByCallId);
                break;
        }
    }

    private async Task HandleInitiatedAsync(
        CallLifecycleEvent callLifecycleEvent, IServiceScope scope, ITelephonyCallService telephonyCallService)
    {
        if (string.IsNullOrWhiteSpace(callLifecycleEvent.CallerExtension)
            || string.IsNullOrWhiteSpace(callLifecycleEvent.CalleeExtension))
        {
            this.logger.LogWarning(
                "Skipping call history for call {CallId} — no caller extension reported by Asterisk.",
                callLifecycleEvent.CallId);

            return;
        }

        ITelephonyIdentityService telephonyIdentityService =
            scope.ServiceProvider.GetRequiredService<ITelephonyIdentityService>();

        TelephonyIdentity? callerIdentity =
            await telephonyIdentityService.RetrieveTelephonyIdentityBySipUsernameAsync(
                callLifecycleEvent.CallerExtension);

        TelephonyIdentity? calleeIdentity =
            await telephonyIdentityService.RetrieveTelephonyIdentityBySipUsernameAsync(
                callLifecycleEvent.CalleeExtension);

        // Expected/acceptable in dev — ad hoc test extensions aren't always provisioned through
        // ClientsController. Skip recording rather than fail or block the call itself.
        if (callerIdentity is null || calleeIdentity is null)
        {
            this.logger.LogWarning(
                "Skipping call history for call {CallId} — no TelephonyIdentity provisioned for " +
                "extension {CallerExtension} or {CalleeExtension}.",
                callLifecycleEvent.CallId,
                callLifecycleEvent.CallerExtension,
                callLifecycleEvent.CalleeExtension);

            return;
        }

        TelephonyCall addedTelephonyCall = await telephonyCallService.AddTelephonyCallAsync(new TelephonyCall
        {
            Id = Guid.NewGuid(),
            CallerIdentityId = callerIdentity.Id,
            RecipientIdentityId = calleeIdentity.Id,
            Status = TelephonyCallStatus.Ringing,
            AsteriskChannelId = callLifecycleEvent.CallId,
            AsteriskBridgeId = callLifecycleEvent.AsteriskBridgeId
        });

        this.telephonyCallIdByCallId[callLifecycleEvent.CallId] = addedTelephonyCall.Id;
    }

    private static async Task HandleAnsweredAsync(
        CallLifecycleEvent callLifecycleEvent,
        ITelephonyCallService telephonyCallService,
        ConcurrentDictionary<string, Guid> telephonyCallIdByCallId)
    {
        if (!telephonyCallIdByCallId.TryGetValue(callLifecycleEvent.CallId, out Guid telephonyCallId))
        {
            return;
        }

        TelephonyCall telephonyCall = await telephonyCallService.RetrieveTelephonyCallByIdAsync(telephonyCallId);
        telephonyCall.Status = TelephonyCallStatus.Answered;
        telephonyCall.StartedAt = DateTimeOffset.UtcNow;
        await telephonyCallService.ModifyTelephonyCallAsync(telephonyCall);
    }

    private static async Task HandleEndedAsync(
        CallLifecycleEvent callLifecycleEvent,
        ITelephonyCallService telephonyCallService,
        ConcurrentDictionary<string, Guid> telephonyCallIdByCallId)
    {
        if (!telephonyCallIdByCallId.TryRemove(callLifecycleEvent.CallId, out Guid telephonyCallId))
        {
            return;
        }

        TelephonyCall telephonyCall = await telephonyCallService.RetrieveTelephonyCallByIdAsync(telephonyCallId);

        // Failed stays unused by this pass — nothing in the current event data distinguishes a
        // genuine failure from an unanswered hangup; both map to Missed, a reasonable CDR
        // convention. Revisit if a real need shows up.
        telephonyCall.Status = telephonyCall.Status == TelephonyCallStatus.Answered
            ? TelephonyCallStatus.Completed
            : TelephonyCallStatus.Missed;

        telephonyCall.EndedAt = DateTimeOffset.UtcNow;
        await telephonyCallService.ModifyTelephonyCallAsync(telephonyCall);
    }
}
