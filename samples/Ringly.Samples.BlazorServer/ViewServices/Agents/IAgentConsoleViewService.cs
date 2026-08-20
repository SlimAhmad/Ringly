using Ringly.Samples.BlazorServer.Models.Agents;

namespace Ringly.Samples.BlazorServer.ViewServices.Agents;

public interface IAgentConsoleViewService : IDisposable
{
    event Action? StateChanged;

    string AgentAppName { get; set; }
    bool IsAvailable { get; }
    string StatusMessage { get; }
    string StatusMessageColorClass { get; }

    IReadOnlyList<AgentBroadcastInfo> Broadcasts { get; }

    ValueTask InitializeAsync();
    ValueTask ToggleAvailabilityAsync();
    ValueTask ClaimAsync(string channelId);
}
