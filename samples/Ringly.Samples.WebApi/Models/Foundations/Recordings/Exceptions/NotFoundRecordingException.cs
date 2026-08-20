using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

public class NotFoundRecordingException : Xeption
{
    public NotFoundRecordingException(Guid recordingId)
        : base($"Could not find recording with id: {recordingId}.")
    { }

    public NotFoundRecordingException(string recordingName)
        : base($"Could not find recording with name: {recordingName}.")
    { }
}
