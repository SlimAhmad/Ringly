using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Models;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.Samples.WebApi;

// Bridges calls a client dials directly (e.g. samples/Ringly.Samples.Maui registering as "1000"
// and calling "1001") — docker/asterisk/config/extensions.conf's [ride_hailing] context hands
// every such call to Stasis(ride_hailing_app, ${EXTEN}), which without a listener here just
// leaves the caller parked with nothing happening (confirmed: no ringing, no decline, no
// timeout). StartCallSessionAsync/RouteToQueueAsync don't need this router — they originate and
// bridge both their own channels directly via the broker's HTTP API, with no dependency on
// Stasis events at all.
public class RideHailingCallRouter : BackgroundService, ICallLifecycleEventSource
{
    private const string MixingBridgeType = "mixing";
    private const string UpChannelState = "Up";

    private readonly IAsteriskBroker asteriskBroker;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<RideHailingCallRouter> logger;

    // Tracks channels this router originated itself (the callee leg) so the caller's leg isn't
    // mistaken for one, and carries what's needed to finish connecting the call once the callee
    // genuinely answers (see HandleChannelStateChangeAsync) — not on its own StasisStart, which
    // fires the instant Asterisk creates the channel, well before the real device has rung, let
    // alone answered.
    private readonly ConcurrentDictionary<string, PendingCall> pendingCallByCalleeChannelId = new();

    // Confirmed live: a client that crashes or is force-closed mid-call never sends a SIP BYE, and
    // this router previously had no way to notice — the callee leg it dialed (or, once answered,
    // either leg of the bridge) just sat open in Asterisk indefinitely. 14 such leaked channels
    // accumulated over one session's worth of crashed test runs, and once an endpoint had enough of
    // them, Asterisk started instantly declining (SIP 603) every new call to it — no ringing at all
    // on either side. Populated for BOTH directions the instant a call is dialed (not just once
    // answered) so a caller vanishing before the callee even rings is caught too, not only crashes
    // after connect. On either channel's StasisEnd, its peer (looked up here) gets hung up and both
    // entries removed — see HandleStasisEndAsync.
    private readonly ConcurrentDictionary<string, string> peerChannelIdByChannelId = new();

    // Channel ID (either leg) -> the CALLER's own channel ID, i.e. the stable CallId used on
    // CallLifecycleEvent — populated for both legs at dial time (same dual-key pattern as
    // peerChannelIdByChannelId above), since HandleStasisEndAsync only ever sees a single raw
    // channel ID and needs to recover the original CallId regardless of which leg ends first.
    private readonly ConcurrentDictionary<string, string> callIdByChannelId = new();

    private readonly Subject<CallLifecycleEvent> callLifecycleEvents = new();

    public IObservable<CallLifecycleEvent> StreamCallLifecycleEvents() => this.callLifecycleEvents.AsObservable();

    // Lets AgentsController register a claimed customer<->agent pair into this router's own
    // hangup-cascade watch (peerChannelIdByChannelId/HandleStasisEndAsync below) without
    // duplicating that logic. Confirmed live as a real gap otherwise: when either side of a
    // claimed call hung up, the other side's channel just stayed connected indefinitely — nothing
    // was watching for one leg's StasisEnd to hang up the other, unlike ride-hailing's own
    // directly-dialed calls, which this router already handles. No CallId/CallLifecycleEvent
    // tracking for this pair — that's specific to ride-hailing's own call-history reporting, not
    // needed here.
    public void RegisterPeerChannels(string channelIdA, string channelIdB)
    {
        this.peerChannelIdByChannelId[channelIdA] = channelIdB;
        this.peerChannelIdByChannelId[channelIdB] = channelIdA;
    }

    private sealed record PendingCall(string BridgeId, string CallerChannelId);

    public RideHailingCallRouter(
        IAsteriskBroker asteriskBroker,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<RideHailingCallRouter> logger)
    {
        this.asteriskBroker = asteriskBroker;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IDisposable stasisSubscription = this.asteriskBroker.StreamStasisStartEvents()
            .Subscribe(stasisStartEvent => this.OnStasisStart(stasisStartEvent));

        IDisposable stasisEndSubscription = this.asteriskBroker.StreamStasisEndEvents()
            .Subscribe(stasisEndEvent => this.OnStasisEnd(stasisEndEvent));

        IDisposable stateChangeSubscription = this.asteriskBroker.StreamChannelStateChangeEvents()
            .Subscribe(stateChangeEvent => this.OnChannelStateChange(stateChangeEvent));

        stoppingToken.Register(() =>
        {
            stasisSubscription.Dispose();
            stasisEndSubscription.Dispose();
            stateChangeSubscription.Dispose();
        });

        return Task.CompletedTask;
    }

    private async void OnStasisStart(StasisStartEvent stasisStartEvent)
    {
        try
        {
            await this.HandleStasisStartAsync(stasisStartEvent);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to route Stasis-dialed call for channel {ChannelId}",
                stasisStartEvent.ChannelId);
        }
    }

    private async void OnStasisEnd(StasisEndEvent stasisEndEvent)
    {
        try
        {
            await this.HandleStasisEndAsync(stasisEndEvent);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to clean up peer channel after StasisEnd for channel {ChannelId}",
                stasisEndEvent.ChannelId);
        }
    }

    private async void OnChannelStateChange(ChannelStateChangeEvent stateChangeEvent)
    {
        try
        {
            await this.HandleChannelStateChangeAsync(stateChangeEvent);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to bridge answered channel {ChannelId}",
                stateChangeEvent.ChannelId);
        }
    }

    private async Task HandleStasisStartAsync(StasisStartEvent stasisStartEvent)
    {
        if (this.pendingCallByCalleeChannelId.ContainsKey(stasisStartEvent.ChannelId))
        {
            // Our own originated (callee) leg entering Stasis — do nothing here. Answering or
            // bridging it now (even without an explicit answer — joining a mixing bridge alone
            // was confirmed to make Asterisk treat that as an implicit answer) is what caused
            // every call to "answer" within 60-160ms of being dialed, before the callee's real
            // device had even rung. Waiting for its ChannelStateChange to "Up" instead means the
            // callee's own client genuinely has to answer first.
            return;
        }

        if (stasisStartEvent.Args.Count == 0)
        {
            // Not a client-dialed entry into the ride_hailing dialplan (e.g. a leg
            // Ringly.Samples.WebApi originated itself via StartCallSessionAsync/RouteToQueueAsync,
            // which bridges its own channels directly and doesn't need this router).
            return;
        }

        string targetExtension = stasisStartEvent.Args[0];

        if (await this.TryHandleQueueTransferAsync(stasisStartEvent.ChannelId, targetExtension))
        {
            return;
        }

        // Send ringing indication (SIP 180 Ringing) rather than answering — answering this leg
        // immediately was confirmed to make the caller's own client transition straight to an
        // "answered" call state well before the callee had even started ringing, since ARI's
        // answer action sends a real 200 OK back through the caller's SIP dialog. The caller
        // leg is only actually answered once the callee genuinely answers (see
        // HandleChannelStateChangeAsync below).
        await this.asteriskBroker.RingChannelAsync(stasisStartEvent.ChannelId);
        Bridge bridge = await this.asteriskBroker.InsertBridgeAsync(MixingBridgeType);

        this.callIdByChannelId[stasisStartEvent.ChannelId] = stasisStartEvent.ChannelId;

        this.callLifecycleEvents.OnNext(new CallLifecycleEvent(
            CallId: stasisStartEvent.ChannelId,
            Phase: CallLifecyclePhase.Initiated,
            CallerExtension: stasisStartEvent.CallerExtension,
            CalleeExtension: targetExtension,
            AsteriskBridgeId: bridge.Id));

        Channel targetChannel = await this.asteriskBroker.InsertChannelAsync($"PJSIP/{targetExtension}");
        this.pendingCallByCalleeChannelId[targetChannel.ChannelId] =
            new PendingCall(bridge.Id, stasisStartEvent.ChannelId);

        this.peerChannelIdByChannelId[stasisStartEvent.ChannelId] = targetChannel.ChannelId;
        this.peerChannelIdByChannelId[targetChannel.ChannelId] = stasisStartEvent.ChannelId;

        this.callIdByChannelId[targetChannel.ChannelId] = stasisStartEvent.ChannelId;
    }

    // Row #38d — lets Dograh's native Call Transfer tool target any department/queue by name
    // (e.g. "support", "billing") without a dialplan edit per department: extensions.conf's
    // _[a-z]. pattern hands any letter-started extension here, and this checks whether it's a
    // real registered queue before falling through to a normal callee-dial. A fresh channel
    // Dograh's own tool dials itself (never one being moved mid-flight), so none of
    // EscalateToQueueAsync's ARI /move race with Dograh's own app applies. IQueueRegistry is
    // Scoped; this class is a singleton BackgroundService, so a fresh scope is created per event
    // rather than injecting it directly (same DI-lifetime fix as RecordingFinalizer). Real callee
    // extensions (1000-1004, 1002) are all digit-only and already handled by the _X. pattern
    // above, which never reaches this method — so there's no ambiguity between "a queue name" and
    // "a real callee extension" in practice, despite this checking every targetExtension.
    private async Task<bool> TryHandleQueueTransferAsync(string channelId, string targetExtension)
    {
        using IServiceScope scope = this.serviceScopeFactory.CreateScope();
        IQueueRegistry queueRegistry = scope.ServiceProvider.GetRequiredService<IQueueRegistry>();
        HoldingBridge? holdingBridge = await queueRegistry.RetrieveByNameAsync(targetExtension);

        if (holdingBridge is null)
        {
            return false;
        }

        await this.asteriskBroker.AnswerChannelAsync(channelId);
        await this.asteriskBroker.AddChannelToBridgeAsync(holdingBridge.BridgeId, channelId);
        return true;
    }

    private async Task HandleStasisEndAsync(StasisEndEvent stasisEndEvent)
    {
        if (!this.peerChannelIdByChannelId.TryRemove(stasisEndEvent.ChannelId, out string? peerChannelId))
        {
            return;
        }

        this.peerChannelIdByChannelId.TryRemove(peerChannelId, out _);
        this.pendingCallByCalleeChannelId.TryRemove(stasisEndEvent.ChannelId, out _);
        this.pendingCallByCalleeChannelId.TryRemove(peerChannelId, out _);

        if (this.callIdByChannelId.TryRemove(stasisEndEvent.ChannelId, out string? callId))
        {
            this.callIdByChannelId.TryRemove(peerChannelId, out _);
            this.callLifecycleEvents.OnNext(new CallLifecycleEvent(CallId: callId, Phase: CallLifecyclePhase.Ended));
        }

        // Best-effort: the peer may already be gone (e.g. both legs of a bridge left Stasis around
        // the same time, or the peer already hung up on its own) — ARI's DELETE 404s in that case,
        // which is fine, there's nothing left to clean up. Not caught here specifically; it's
        // caught (and logged) by OnStasisEnd's wrapper, same as every other handler in this class.
        await this.asteriskBroker.HangupChannelAsync(peerChannelId);
    }

    private async Task HandleChannelStateChangeAsync(ChannelStateChangeEvent stateChangeEvent)
    {
        if (stateChangeEvent.State != UpChannelState)
        {
            return;
        }

        if (this.pendingCallByCalleeChannelId.TryRemove(stateChangeEvent.ChannelId, out PendingCall? pendingCall))
        {
            await this.asteriskBroker.AnswerChannelAsync(pendingCall.CallerChannelId);
            await this.asteriskBroker.AddChannelToBridgeAsync(pendingCall.BridgeId, pendingCall.CallerChannelId);
            await this.asteriskBroker.AddChannelToBridgeAsync(pendingCall.BridgeId, stateChangeEvent.ChannelId);

            // pendingCall.CallerChannelId IS the CallId (the caller's own channel ID) — no lookup
            // needed here, unlike HandleStasisEndAsync below which only ever sees a single raw
            // channel ID that could belong to either leg.
            this.callLifecycleEvents.OnNext(new CallLifecycleEvent(
                CallId: pendingCall.CallerChannelId,
                Phase: CallLifecyclePhase.Answered));
        }
    }
}
