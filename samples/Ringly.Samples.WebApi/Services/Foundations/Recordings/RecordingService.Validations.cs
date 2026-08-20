using Ringly.Samples.WebApi.Models.Foundations.Recordings;
using Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

namespace Ringly.Samples.WebApi.Services.Foundations.Recordings;

public partial class RecordingService
{
    private static void ValidateRecordingOnAdd(Recording recording)
    {
        ValidateRecordingIsNotNull(recording);

        Validate(
            (Rule: IsInvalid(recording.Id), Parameter: nameof(Recording.Id)),
            (Rule: IsInvalid(recording.BridgeId), Parameter: nameof(Recording.BridgeId)),
            (Rule: IsInvalid(recording.RecordingName), Parameter: nameof(Recording.RecordingName)),
            (Rule: IsInvalid(recording.Format), Parameter: nameof(Recording.Format)),
            (Rule: IsInvalid(recording.State), Parameter: nameof(Recording.State)));
    }

    private static void ValidateRecordingOnModify(Recording recording)
    {
        ValidateRecordingIsNotNull(recording);

        Validate(
            (Rule: IsInvalid(recording.Id), Parameter: nameof(Recording.Id)),
            (Rule: IsInvalid(recording.BridgeId), Parameter: nameof(Recording.BridgeId)),
            (Rule: IsInvalid(recording.RecordingName), Parameter: nameof(Recording.RecordingName)),
            (Rule: IsInvalid(recording.Format), Parameter: nameof(Recording.Format)),
            (Rule: IsInvalid(recording.State), Parameter: nameof(Recording.State)));
    }

    private static void ValidateRecordingId(Guid recordingId) =>
        Validate((Rule: IsInvalid(recordingId), Parameter: nameof(Recording.Id)));

    private static void ValidateRecordingName(string recordingName) =>
        Validate((Rule: IsInvalid(recordingName), Parameter: nameof(Recording.RecordingName)));

    private static void ValidateRecordingIsNotNull(Recording? recording)
    {
        if (recording is null)
        {
            throw new NullRecordingException();
        }
    }

    private static void ValidateStorageRecordingExists(Recording? maybeRecording, Guid recordingId)
    {
        if (maybeRecording is null)
        {
            throw new NotFoundRecordingException(recordingId);
        }
    }

    private static dynamic IsInvalid(Guid id) => new
    {
        Condition = id == default,
        Message = "Id is required"
    };

    private static dynamic IsInvalid(string text) => new
    {
        Condition = string.IsNullOrWhiteSpace(text),
        Message = "Text is required"
    };

    private static void Validate(params (dynamic Rule, string Parameter)[] validations)
    {
        var invalidRecordingException = new InvalidRecordingException();

        foreach ((dynamic rule, string parameter) in validations)
        {
            if (rule.Condition)
            {
                invalidRecordingException.UpsertDataList(key: parameter, value: rule.Message);
            }
        }

        invalidRecordingException.ThrowIfContainsErrors();
    }
}
