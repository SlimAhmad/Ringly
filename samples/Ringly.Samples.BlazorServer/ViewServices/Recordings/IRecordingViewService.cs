using Ringly.Samples.BlazorServer.Models.Recordings;

namespace Ringly.Samples.BlazorServer.ViewServices.Recordings;

// The single dependency RecordingsPanel.razor (the Core Component) integrates with — mirrors
// IDepartmentsViewService's own role for DepartmentsPanel. Owns starting/listing/controlling
// recordings against whatever bridge the agent's active claimed call is using (read from
// IAgentConsoleViewService.CurrentBridgeId — a view-service-to-view-service dependency, not a
// second Core Component dependency; the-standard-architecture's "single dependency" rule is about
// a Core Component's own relationship to its view service).
public interface IRecordingViewService : IDisposable
{
    event Action? StateChanged;

    string NewRecordingName { get; set; }
    string NewRecordingFormat { get; set; }
    string StatusMessage { get; }
    string StatusMessageColorClass { get; }
    bool IsBusy { get; }
    string? CurrentBridgeId { get; }
    IReadOnlyList<RecordingRow> Recordings { get; }

    ValueTask InitializeAsync();
    ValueTask CreateRecordingAsync();
    ValueTask PauseAsync(string recordingName);
    ValueTask UnpauseAsync(string recordingName);
    ValueTask StopAsync(string recordingName);
    ValueTask CancelAsync(string recordingName);
    ValueTask RemoveAsync(string recordingName);
    ValueTask PlayAsync(string recordingName);
}
