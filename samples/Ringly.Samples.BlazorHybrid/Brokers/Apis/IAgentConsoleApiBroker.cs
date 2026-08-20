using Ringly.Samples.BlazorHybrid.Models.Agents;

namespace Ringly.Samples.BlazorHybrid.Brokers.Apis;

// Liaison between this app and Ringly.Samples.WebApi's AgentsController — no business logic here,
// just the HTTP/SSE calls themselves, matching that controller's real routes.
public interface IAgentConsoleApiBroker
{
    ValueTask PostAvailabilityAsync(string agentAppName, bool isAvailable);
    ValueTask<ClaimResult> PostClaimAsync(string agentAppName, string channelId);

    // Not a ValueTask<T> — this is a genuinely long-lived stream (the-standard-architecture's
    // asynchronization-abstraction rule is about uniform async *return shape* for request/response
    // calls; a push feed with no natural "one result" is exactly what IAsyncEnumerable is for).
    IAsyncEnumerable<AgentBroadcastInfo> StreamBroadcastsAsync(CancellationToken cancellationToken);
}
