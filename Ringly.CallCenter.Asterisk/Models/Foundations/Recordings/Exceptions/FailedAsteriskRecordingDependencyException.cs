using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

public class FailedAsteriskRecordingDependencyException : Xeption
{
    public FailedAsteriskRecordingDependencyException(Exception innerException)
        : base("Failed Asterisk recording dependency error occurred, contact support.", innerException)
    { }
}
