using Ringly.Samples.BlazorServer.Models.Recordings;

namespace Ringly.Samples.BlazorServer.Brokers.Apis;

// Liaison between this app and Ringly.Samples.WebApi's RecordingsController — no business logic
// here, just the HTTP calls themselves, matching that controller's real routes.
public interface IRecordingApiBroker
{
    ValueTask<IReadOnlyList<RecordingRow>> GetRecordingsAsync();
    ValueTask PostRecordingAsync(string bridgeId, string recordingName, string format);
    ValueTask PostPauseAsync(string recordingName);
    ValueTask PostUnpauseAsync(string recordingName);
    ValueTask PostStopAsync(string recordingName);
    ValueTask PostCancelAsync(string recordingName);
    ValueTask DeleteRecordingAsync(string recordingName);
}
