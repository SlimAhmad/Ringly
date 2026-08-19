using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

public class FailedRecordingServiceException : Xeption
{
    public FailedRecordingServiceException(Exception innerException)
        : base("Failed recording service error occurred, contact support.", innerException)
    { }
}
