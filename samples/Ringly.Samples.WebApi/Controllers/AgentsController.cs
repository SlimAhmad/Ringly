using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

namespace Ringly.Samples.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : RESTFulController
{
    private readonly ICallCenterProvider callCenterProvider;

    public AgentsController(ICallCenterProvider callCenterProvider) =>
        this.callCenterProvider = callCenterProvider;

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

    [HttpPost("{agentAppName}/claim/{channelId}")]
    public async ValueTask<ActionResult<ClaimResult>> PostClaimAsync(string agentAppName, string channelId)
    {
        try
        {
            ClaimResult claimResult = await this.callCenterProvider.ClaimCallAsync(channelId, agentAppName);

            return this.Ok(value: claimResult);
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

        var completionSource = new TaskCompletionSource();

        using CancellationTokenRegistration registration =
            cancellationToken.Register(() => completionSource.TrySetResult());

        using IDisposable subscription = this.callCenterProvider.StreamCallBroadcasts().Subscribe(
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
