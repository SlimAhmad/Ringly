using Ringly.Samples.BlazorServer.Models.Agents;

namespace Ringly.Samples.BlazorServer.Brokers.Apis;

// Liaison between this app and Ringly.Samples.WebApi's AgentsController — no business logic here,
// just the HTTP/SSE calls themselves, matching that controller's real routes.
public interface IAgentConsoleApiBroker
{
    ValueTask PostAvailabilityAsync(string agentAppName, bool isAvailable);
    ValueTask PostClaimAsync(string agentAppName, string channelId);
    IAsyncEnumerable<AgentBroadcastInfo> StreamBroadcastsAsync(CancellationToken cancellationToken);
}
