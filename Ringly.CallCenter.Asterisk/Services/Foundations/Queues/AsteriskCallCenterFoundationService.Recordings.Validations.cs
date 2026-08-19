using Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService
{
    private static void ValidateRecordingName(string recordingName)
    {
        if (string.IsNullOrWhiteSpace(recordingName))
        {
            var invalidRecordingRequestException = new InvalidRecordingRequestException();

            invalidRecordingRequestException.UpsertDataList(
                key: nameof(recordingName),
                value: "Value is required");

            invalidRecordingRequestException.ThrowIfContainsErrors();
        }
    }

    private static void ValidateInsertRecordingRequest(string bridgeId, string recordingName, string format)
    {
        var invalidRecordingRequestException = new InvalidRecordingRequestException();

        if (string.IsNullOrWhiteSpace(bridgeId))
        {
            invalidRecordingRequestException.UpsertDataList(key: nameof(bridgeId), value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(recordingName))
        {
            invalidRecordingRequestException.UpsertDataList(key: nameof(recordingName), value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(format))
        {
            invalidRecordingRequestException.UpsertDataList(key: nameof(format), value: "Value is required");
        }

        invalidRecordingRequestException.ThrowIfContainsErrors();
    }

    private static void ValidateCopyStoredRecordingRequest(string recordingName, string destinationName)
    {
        var invalidRecordingRequestException = new InvalidRecordingRequestException();

        if (string.IsNullOrWhiteSpace(recordingName))
        {
            invalidRecordingRequestException.UpsertDataList(key: nameof(recordingName), value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(destinationName))
        {
            invalidRecordingRequestException.UpsertDataList(key: nameof(destinationName), value: "Value is required");
        }

        invalidRecordingRequestException.ThrowIfContainsErrors();
    }
}
