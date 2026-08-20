using Ringly.Samples.BlazorServer.Brokers.Apis;
using Ringly.Samples.BlazorServer.Models.Agents;

namespace Ringly.Samples.BlazorServer.ViewServices.Agents;

public sealed class AgentConsoleViewService : IAgentConsoleViewService
{
    private readonly IAgentConsoleApiBroker agentConsoleApiBroker;
    private readonly List<AgentBroadcastInfo> broadcasts = [];
    private readonly CancellationTokenSource listenCancellationSource = new();

    public event Action? StateChanged;

    public string AgentAppName { get; set; } = string.Empty;
    public bool IsAvailable { get; private set; }
    public string StatusMessage { get; private set; } = string.Empty;
    public string StatusMessageColorClass { get; private set; } = string.Empty;

    public IReadOnlyList<AgentBroadcastInfo> Broadcasts => this.broadcasts;

    public AgentConsoleViewService(IAgentConsoleApiBroker agentConsoleApiBroker) =>
        this.agentConsoleApiBroker = agentConsoleApiBroker;

    public ValueTask InitializeAsync()
    {
        _ = this.ListenForBroadcastsAsync(this.listenCancellationSource.Token);
        return ValueTask.CompletedTask;
    }

    private async Task ListenForBroadcastsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (AgentBroadcastInfo broadcast in
                this.agentConsoleApiBroker.StreamBroadcastsAsync(cancellationToken))
            {
                this.broadcasts.Insert(0, broadcast);
                this.OnStateChanged();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on Dispose — nothing to report.
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Broadcast stream disconnected: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
            this.OnStateChanged();
        }
    }

    public async ValueTask ToggleAvailabilityAsync()
    {
        if (string.IsNullOrWhiteSpace(this.AgentAppName))
        {
            return;
        }

        try
        {
            await this.agentConsoleApiBroker.PostAvailabilityAsync(this.AgentAppName, !this.IsAvailable);
            this.IsAvailable = !this.IsAvailable;
            this.StatusMessage = this.IsAvailable ? "Available" : "Unavailable";
            this.StatusMessageColorClass = this.IsAvailable ? "text-emerald-400" : "text-slate-400";
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Availability update failed: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
        }

        this.OnStateChanged();
    }

    public async ValueTask ClaimAsync(string channelId)
    {
        if (string.IsNullOrWhiteSpace(this.AgentAppName))
        {
            this.StatusMessage = "Set an agent app name before claiming a call.";
            this.StatusMessageColorClass = "text-red-400";
            this.OnStateChanged();
            return;
        }

        try
        {
            await this.agentConsoleApiBroker.PostClaimAsync(this.AgentAppName, channelId);
            this.broadcasts.RemoveAll(broadcast => broadcast.ChannelId == channelId);
            this.StatusMessage = $"Claimed {channelId}.";
            this.StatusMessageColorClass = "text-emerald-400";
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Claim failed: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
        }

        this.OnStateChanged();
    }

    private void OnStateChanged() => this.StateChanged?.Invoke();

    public void Dispose()
    {
        this.listenCancellationSource.Cancel();
        this.listenCancellationSource.Dispose();
    }
}
