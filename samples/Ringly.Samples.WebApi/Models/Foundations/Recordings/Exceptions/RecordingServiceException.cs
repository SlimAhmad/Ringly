using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

public class RecordingServiceException : Xeption
{
    public RecordingServiceException(Xeption innerException)
        : base("Recording service error occurred, contact support.", innerException)
    { }
}
