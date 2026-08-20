using Ringly.Samples.BlazorHybrid.Brokers.Apis;
using Ringly.Samples.BlazorHybrid.Models.Agents;

namespace Ringly.Samples.BlazorHybrid.ViewServices.Agents;

public sealed class AgentConsoleViewService : IAgentConsoleViewService
{
    // This view service is a Singleton (registered once for the app's whole lifetime), so
    // InitializeAsync only ever runs once — a single unhandled failure in the stream (a dropped
    // Wi-Fi connection, the WebApi restarting, a DNS blip) used to end ListenForBroadcastsAsync
    // permanently, leaving "Broadcast stream disconnected" on screen forever with no way to
    // recover short of force-closing the whole app. Confirmed live this session: multiple Wi-Fi
    // network switches left the console stuck on a stale disconnect message even after the
    // underlying connectivity (and a separate server-side header-flush bug, see WebApi issue #268)
    // were both fixed — the loop had simply already given up. Retrying with backoff instead means
    // a transient failure recovers on its own instead of requiring a full app relaunch.
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

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
        // Fire-and-forget, matches this session's established async-void-with-internal-try/catch
        // idiom (RideHailingCallRouter/TelephonyCallTrackingService) — a stream that runs for the
        // lifetime of the component, not a request/response call this method should itself await.
        _ = this.ListenForBroadcastsAsync(this.listenCancellationSource.Token);
        return ValueTask.CompletedTask;
    }

    private async Task ListenForBroadcastsAsync(CancellationToken cancellationToken)
    {
        TimeSpan retryDelay = InitialRetryDelay;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                this.StatusMessage = "Listening for broadcasts.";
                this.StatusMessageColorClass = "text-emerald-400";
                this.OnStateChanged();

                await foreach (AgentBroadcastInfo broadcast in
                    this.agentConsoleApiBroker.StreamBroadcastsAsync(cancellationToken))
                {
                    this.broadcasts.Insert(0, broadcast);
                    this.OnStateChanged();
                }

                // The stream ended without an exception (e.g. the server completed it) — still
                // worth retrying rather than going quiet forever, same reasoning as the catch
                // block below.
                retryDelay = InitialRetryDelay;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected on Dispose — the console isn't in use anymore, nothing to report.
                return;
            }
            catch (Exception exception)
            {
                this.StatusMessage = $"Broadcast stream disconnected: {exception.Message} — retrying in {retryDelay.TotalSeconds:0}s.";
                this.StatusMessageColorClass = "text-red-400";
                this.OnStateChanged();
            }

            try
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, MaxRetryDelay.TotalSeconds));
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
