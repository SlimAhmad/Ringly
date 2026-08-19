using Ringly.Asterisk.Models;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService
{
    public ValueTask<RecordingInfo> InsertRecordingAsync(string bridgeId, string recordingName, string format) =>
    TryCatchRecordingInfo(async () =>
    {
        ValidateInsertRecordingRequest(bridgeId, recordingName, format);

        LiveRecording liveRecording =
            await this.asteriskBroker.InsertRecordingAsync(bridgeId, recordingName, format);

        return new RecordingInfo { Name = liveRecording.Name, State = liveRecording.State };
    });

    public ValueTask PauseRecordingAsync(string recordingName) =>
    TryCatchRecording(async () =>
    {
        ValidateRecordingName(recordingName);
        await this.asteriskBroker.PauseRecordingAsync(recordingName);
    });

    public ValueTask UnpauseRecordingAsync(string recordingName) =>
    TryCatchRecording(async () =>
    {
        ValidateRecordingName(recordingName);
        await this.asteriskBroker.UnpauseRecordingAsync(recordingName);
    });

    public ValueTask StopRecordingAsync(string recordingName) =>
    TryCatchRecording(async () =>
    {
        ValidateRecordingName(recordingName);
        await this.asteriskBroker.StopRecordingAsync(recordingName);
    });

    public ValueTask CancelRecordingAsync(string recordingName) =>
    TryCatchRecording(async () =>
    {
        ValidateRecordingName(recordingName);
        await this.asteriskBroker.CancelRecordingAsync(recordingName);
    });

    public ValueTask DeleteStoredRecordingAsync(string recordingName) =>
    TryCatchRecording(async () =>
    {
        ValidateRecordingName(recordingName);
        await this.asteriskBroker.DeleteStoredRecordingAsync(recordingName);
    });

    public ValueTask CopyStoredRecordingAsync(string recordingName, string destinationName) =>
    TryCatchRecording(async () =>
    {
        ValidateCopyStoredRecordingRequest(recordingName, destinationName);
        await this.asteriskBroker.CopyStoredRecordingAsync(recordingName, destinationName);
    });
}
