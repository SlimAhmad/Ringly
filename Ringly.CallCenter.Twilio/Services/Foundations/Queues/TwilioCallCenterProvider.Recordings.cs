using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.CallCenter.Twilio.Services.Foundations.Queues;

public partial class TwilioCallCenterProvider
{
    // Twilio has a real Recording API (start/pause/resume/stop, list/delete stored recordings) —
    // this isn't a platform gap the way StreamTransferRequests is, it's just not built yet. Needs
    // new ITwilioBroker methods before a real implementation can land here. See
    // TwilioCallCenterProvider.Agents.cs's own comment for the same reasoning.
    public ValueTask<RecordingInfo> InsertRecordingAsync(string bridgeId, string recordingName, string format) =>
        throw new NotSupportedException(
            "InsertRecordingAsync is not yet implemented for Twilio — needs a Recording API broker method.");

    public ValueTask PauseRecordingAsync(string recordingName) =>
        throw new NotSupportedException(
            "PauseRecordingAsync is not yet implemented for Twilio — needs a Recording API broker method.");

    public ValueTask UnpauseRecordingAsync(string recordingName) =>
        throw new NotSupportedException(
            "UnpauseRecordingAsync is not yet implemented for Twilio — needs a Recording API broker method.");

    public ValueTask StopRecordingAsync(string recordingName) =>
        throw new NotSupportedException(
            "StopRecordingAsync is not yet implemented for Twilio — needs a Recording API broker method.");

    public ValueTask CancelRecordingAsync(string recordingName) =>
        throw new NotSupportedException(
            "CancelRecordingAsync is not yet implemented for Twilio — needs a Recording API broker method.");

    public ValueTask DeleteStoredRecordingAsync(string recordingName) =>
        throw new NotSupportedException(
            "DeleteStoredRecordingAsync is not yet implemented for Twilio — needs a Recording API broker method.");

    public ValueTask CopyStoredRecordingAsync(string recordingName, string destinationName) =>
        throw new NotSupportedException(
            "CopyStoredRecordingAsync is not yet implemented for Twilio — needs a Recording API broker method.");
}
