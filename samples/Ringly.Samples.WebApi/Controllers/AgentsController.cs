using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;
using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Samples.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : RESTFulController
{
    private readonly ICallCenterProvider callCenterProvider;
    private readonly ICallProvider callProvider;
    private readonly SupportQueueBroadcastRegistry supportQueueBroadcastRegistry;
    private readonly RideHailingCallRouter rideHailingCallRouter;

    public AgentsController(
        ICallCenterProvider callCenterProvider,
        ICallProvider callProvider,
        SupportQueueBroadcastRegistry supportQueueBroadcastRegistry,
        RideHailingCallRouter rideHailingCallRouter)
    {
        this.callCenterProvider = callCenterProvider;
        this.callProvider = callProvider;
        this.supportQueueBroadcastRegistry = supportQueueBroadcastRegistry;
        this.rideHailingCallRouter = rideHailingCallRouter;
    }

    [HttpPost("{agentAppName}/availability")]
    public async ValueTask<ActionResult> PostAvailabilityAsync(
        string agentAppName, [FromBody] SetAvailabilityRequest request)
    {
        try
        {
            await this.callCenterProvider.SetAgentAvailabilityAsync(agentAppName, request.IsAvailable);

            return this.Ok();
        }
        catch (AgentValidationException agentValidationException)
        {
            return this.BadRequest(agentValidationException.InnerException);
        }
        catch (AgentDependencyValidationException agentDependencyValidationException)
        {
            return this.BadRequest(agentDependencyValidationException.InnerException);
        }
        catch (AgentDependencyException agentDependencyException)
        {
            return this.InternalServerError(agentDependencyException);
        }
        catch (AgentServiceException agentServiceException)
        {
            return this.InternalServerError(agentServiceException);
        }
    }

    // Claims a customer waiting via the real SupportController flow (see
    // SupportQueueBroadcastRegistry's own comment for why this doesn't go through
    // ICallCenterProvider.ClaimCallAsync) — arbitration is atomic in the registry, then the
    // winning agent's own channel (agentAppName IS their SIP extension — see the WebApi README's
    // customer-support walkthrough for this convention) is originated and bridged in.
    [HttpPost("{agentAppName}/claim/{channelId}")]
    public async ValueTask<ActionResult<ClaimResult>> PostClaimAsync(string agentAppName, string channelId)
    {
        ClaimAttemptResult claimAttemptResult = this.supportQueueBroadcastRegistry.TryClaim(channelId, out string? bridgeId);

        if (claimAttemptResult == ClaimAttemptResult.NotFound)
        {
            return this.NotFound();
        }

        if (claimAttemptResult == ClaimAttemptResult.AlreadyClaimed)
        {
            return this.Conflict();
        }

        try
        {
            Channel agentChannel =
                await this.callProvider.ConnectAgentToQueueAsync(bridgeId!, channelId, agentAppName);

            // Confirmed live as a real gap: without this, hanging up on either side left the
            // other side's channel connected indefinitely — nothing was watching for one leg to
            // end and hang up the other, unlike RideHailingCallRouter's own directly-dialed
            // calls. Reuses that router's existing hangup-cascade watch instead of duplicating it.
            this.rideHailingCallRouter.RegisterPeerChannels(channelId, agentChannel.ChannelId);

            return this.Ok(value: new ClaimResult { Claimed = true, ChannelId = channelId, BridgeId = bridgeId! });
        }
        // Every branch below releases the claim — confirmed live as a real bug: a failed
        // ConnectAgentToQueueAsync (e.g. the agent's own device never answers within its 30s
        // Stasis-entry timeout) previously left the claim permanently burned, since TryClaim above
        // had already marked the channel claimed. Without releasing it here, the customer became
        // unclaimable forever — not even a retry by the same agent could get past AlreadyClaimed.
        catch (CallSessionValidationException callSessionValidationException)
        {
            this.supportQueueBroadcastRegistry.ReleaseClaim(channelId);
            return this.BadRequest(callSessionValidationException.InnerException);
        }
        catch (CallSessionDependencyValidationException callSessionDependencyValidationException)
        {
            this.supportQueueBroadcastRegistry.ReleaseClaim(channelId);
            return this.BadRequest(callSessionDependencyValidationException.InnerException);
        }
        catch (CallProviderDependencyException callProviderDependencyException)
        {
            this.supportQueueBroadcastRegistry.ReleaseClaim(channelId);
            return this.InternalServerError(callProviderDependencyException);
        }
        catch (CallProviderServiceException callProviderServiceException)
        {
            this.supportQueueBroadcastRegistry.ReleaseClaim(channelId);
            return this.InternalServerError(callProviderServiceException);
        }
    }

    // Server-Sent Events rather than WebSockets/SignalR — this is a single one-way push feed
    // (server -> client only, no client-to-server messages needed once subscribed), which is
    // exactly what SSE is for; it needs no extra package, just "text/event-stream" written
    // directly to the response, unlike a SignalR hub or raw WebSocket which would be more
    // machinery than this one-directional stream needs.
    [HttpGet("broadcasts")]
    public async Task GetBroadcastsAsync(CancellationToken cancellationToken)
    {
        this.Response.Headers.ContentType = "text/event-stream";
        this.Response.Headers.CacheControl = "no-cache";

        // Setting the headers above only stages them — Kestrel doesn't actually push a response
        // onto the wire (not even the status line) until the body is flushed, which for this
        // endpoint might not happen for a long time otherwise (StreamCallBroadcasts() only emits
        // on a real incoming call). Confirmed live with instrumented timestamps and a client using
        // HttpCompletionOption.ResponseHeadersRead (the exact semantics AgentConsoleApiBroker
        // uses): StartAsync() alone returns in under a millisecond server-side, yet headers never
        // reached the client — not even over loopback — until this explicit flush. Only once
        // FlushAsync() is added do headers actually arrive (confirmed: ~110ms, previously 5s+
        // timeouts). StartAsync() commits the response logically; FlushAsync() is what actually
        // sends it.
        await this.Response.StartAsync(cancellationToken);
        await this.Response.Body.FlushAsync(cancellationToken);

        var completionSource = new TaskCompletionSource();

        using CancellationTokenRegistration registration =
            cancellationToken.Register(() => completionSource.TrySetResult());

        using IDisposable subscription = this.supportQueueBroadcastRegistry.StreamWaitingCustomers().Subscribe(
            onNext: broadcastEvent =>
            {
                string json = System.Text.Json.JsonSerializer.Serialize(broadcastEvent);
                _ = this.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                _ = this.Response.Body.FlushAsync(cancellationToken);
            },
            onError: _ => completionSource.TrySetResult(),
            onCompleted: () => completionSource.TrySetResult());

        // Blocks (without tying up a thread) until the client disconnects — RequestAborted firing
        // is the only way this stream naturally ends, since StreamCallBroadcasts() itself never
        // completes on its own for as long as the process is running.
        await completionSource.Task;
    }
}

public record SetAvailabilityRequest(bool IsAvailable);
