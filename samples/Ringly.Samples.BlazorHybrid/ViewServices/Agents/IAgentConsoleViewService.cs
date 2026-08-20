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

    IReadOnlyList<AgentBroadcastInfo> Broadcasts { get; }

    ValueTask InitializeAsync();
    ValueTask ToggleAvailabilityAsync();
    ValueTask ClaimAsync(string channelId);
}
