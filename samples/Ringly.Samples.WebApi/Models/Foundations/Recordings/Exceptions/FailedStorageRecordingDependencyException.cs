using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

public class FailedStorageRecordingDependencyException : Xeption
{
    public FailedStorageRecordingDependencyException(Exception innerException)
        : base("Failed recording storage dependency error occurred, contact support.", innerException)
    { }
}
