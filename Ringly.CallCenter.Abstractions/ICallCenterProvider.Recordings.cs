using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.CallCenter.Abstractions;

public partial interface ICallCenterProvider
{
    ValueTask<RecordingInfo> InsertRecordingAsync(string bridgeId, string recordingName, string format);
    ValueTask PauseRecordingAsync(string recordingName);
    ValueTask UnpauseRecordingAsync(string recordingName);
    ValueTask StopRecordingAsync(string recordingName);
    ValueTask CancelRecordingAsync(string recordingName);
    ValueTask DeleteStoredRecordingAsync(string recordingName);
    ValueTask CopyStoredRecordingAsync(string recordingName, string destinationName);
}
