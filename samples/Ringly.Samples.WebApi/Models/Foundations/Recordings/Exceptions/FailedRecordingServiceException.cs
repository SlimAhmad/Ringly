using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

public class FailedRecordingServiceException : Xeption
{
    public FailedRecordingServiceException(Exception innerException)
        : base("Failed recording service error occurred, contact support.", innerException)
    { }
}
