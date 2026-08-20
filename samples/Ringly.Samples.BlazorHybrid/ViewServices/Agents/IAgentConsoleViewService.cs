using Ringly.Samples.BlazorHybrid.Models.Agents;

namespace Ringly.Samples.BlazorHybrid.ViewServices.Agents;

// The single dependency AgentConsole.razor (the Core Component) integrates with.
public interface IAgentConsoleViewService : IDisposable
{
    event Action? StateChanged;

    string AgentAppName { get; set; }
    bool IsAvailable { get; }
    string StatusMessage { get; }
    string StatusMessageColorClass { get; }

    // Set from a successful claim's own response — lets other Core Components (e.g. a Recordings
    // panel) know which bridge the agent's currently-active call is using, without needing their
    // own dependency on this service's Core Component (RecordingViewService reads this directly).
    string? CurrentBridgeId { get; }

    IReadOnlyList<AgentBroadcastInfo> Broadcasts { get; }

    ValueTask InitializeAsync();
    ValueTask ToggleAvailabilityAsync();
    ValueTask ClaimAsync(string channelId);
}
