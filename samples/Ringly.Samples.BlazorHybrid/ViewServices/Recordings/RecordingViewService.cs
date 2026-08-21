using Microsoft.JSInterop;
using Ringly.Samples.BlazorHybrid.Brokers.Apis;
using Ringly.Samples.BlazorHybrid.Models.Recordings;
using Ringly.Samples.BlazorHybrid.ViewServices.Agents;

namespace Ringly.Samples.BlazorHybrid.ViewServices.Recordings;

public sealed class RecordingViewService : IRecordingViewService
{
    private readonly IRecordingApiBroker recordingApiBroker;
    private readonly IAgentConsoleViewService agentConsoleViewService;
    private readonly IJSRuntime jsRuntime;
    private List<RecordingRow> recordings = [];

    public event Action? StateChanged;

    public string NewRecordingName { get; set; } = string.Empty;
    public string NewRecordingFormat { get; set; } = "wav";
    public string StatusMessage { get; private set; } = string.Empty;
    public string StatusMessageColorClass { get; private set; } = string.Empty;
    public bool IsBusy { get; private set; }

    // Reads straight through to the Agent Console's own state rather than caching a snapshot —
    // this always reflects whichever call is active right now, including after a brand new claim
    // the StateChanged subscription below hasn't yet triggered a re-render for.
    public string? CurrentBridgeId => this.agentConsoleViewService.CurrentBridgeId;

    public IReadOnlyList<RecordingRow> Recordings => this.recordings;

    public RecordingViewService(
        IRecordingApiBroker recordingApiBroker,
        IAgentConsoleViewService agentConsoleViewService,
        IJSRuntime jsRuntime)
    {
        this.recordingApiBroker = recordingApiBroker;
        this.agentConsoleViewService = agentConsoleViewService;
        this.jsRuntime = jsRuntime;

        // Picks up a fresh CurrentBridgeId reactively whenever the agent claims a new call, so
        // this panel's "no active call" state clears itself without the operator needing to
        // navigate away and back.
        this.agentConsoleViewService.StateChanged += this.OnStateChanged;
    }

    public async ValueTask InitializeAsync() => await this.LoadRecordingsAsync();

    public async ValueTask CreateRecordingAsync()
    {
        if (string.IsNullOrWhiteSpace(this.CurrentBridgeId))
        {
            this.StatusMessage = "No active call — claim a call before starting a recording.";
            this.StatusMessageColorClass = "text-red-400";
            this.OnStateChanged();
            return;
        }

        if (string.IsNullOrWhiteSpace(this.NewRecordingName))
        {
            return;
        }

        this.IsBusy = true;
        this.OnStateChanged();

        try
        {
            await this.recordingApiBroker.PostRecordingAsync(
                this.CurrentBridgeId, this.NewRecordingName, this.NewRecordingFormat);

            this.NewRecordingName = string.Empty;
            this.StatusMessage = "Recording started.";
            this.StatusMessageColorClass = "text-emerald-400";
            await this.LoadRecordingsAsync();
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Start recording failed: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
        }

        this.IsBusy = false;
        this.OnStateChanged();
    }

    public ValueTask PauseAsync(string recordingName) =>
        this.RunActionAsync(recordingName, this.recordingApiBroker.PostPauseAsync, "Paused");

    public ValueTask UnpauseAsync(string recordingName) =>
        this.RunActionAsync(recordingName, this.recordingApiBroker.PostUnpauseAsync, "Resumed");

    public ValueTask StopAsync(string recordingName) =>
        this.RunActionAsync(recordingName, this.recordingApiBroker.PostStopAsync, "Stopped");

    public ValueTask CancelAsync(string recordingName) =>
        this.RunActionAsync(recordingName, this.recordingApiBroker.PostCancelAsync, "Canceled");

    public ValueTask RemoveAsync(string recordingName) =>
        this.RunActionAsync(recordingName, this.recordingApiBroker.DeleteRecordingAsync, "Removed");

    // The BlobUrl already on each RecordingRow isn't directly playable — the container is
    // private, so this resolves a real signed URL first (see RecordingApiBroker's own comment)
    // and hands it to the platform to actually open, rather than rendering the raw BlobUrl as a
    // link the way this panel did before.
    public async ValueTask PlayAsync(string recordingName)
    {
        this.IsBusy = true;
        this.OnStateChanged();

        try
        {
            Uri accessUrl = await this.recordingApiBroker.GetAccessUrlAsync(recordingName);
            await this.jsRuntime.InvokeVoidAsync("open", accessUrl.ToString(), "_blank");
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Play failed: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
        }

        this.IsBusy = false;
        this.OnStateChanged();
    }

    private async ValueTask RunActionAsync(
        string recordingName, Func<string, ValueTask> action, string successVerb)
    {
        this.IsBusy = true;
        this.OnStateChanged();

        try
        {
            await action(recordingName);
            this.StatusMessage = $"{successVerb} \"{recordingName}\".";
            this.StatusMessageColorClass = "text-emerald-400";
            await this.LoadRecordingsAsync();
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"{successVerb} failed: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
        }

        this.IsBusy = false;
        this.OnStateChanged();
    }

    private async ValueTask LoadRecordingsAsync()
    {
        try
        {
            IReadOnlyList<RecordingRow> retrievedRecordings = await this.recordingApiBroker.GetRecordingsAsync();
            this.recordings = [.. retrievedRecordings];
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Failed to load recordings: {exception.Message}";
            this.StatusMessageColorClass = "text-red-400";
        }

        this.OnStateChanged();
    }

    private void OnStateChanged() => this.StateChanged?.Invoke();

    public void Dispose() => this.agentConsoleViewService.StateChanged -= this.OnStateChanged;
}
