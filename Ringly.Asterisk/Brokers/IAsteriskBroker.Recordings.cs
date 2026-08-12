using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    ValueTask<LiveRecording> InsertRecordingAsync(string bridgeId, string recordingName, string format);
    ValueTask PauseRecordingAsync(string recordingName);
    ValueTask UnpauseRecordingAsync(string recordingName);
    ValueTask StopRecordingAsync(string recordingName);
    ValueTask CancelRecordingAsync(string recordingName);
}
