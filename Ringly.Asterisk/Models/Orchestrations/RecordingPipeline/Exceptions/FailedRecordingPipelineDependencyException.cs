using Xeptions;

namespace Ringly.Asterisk.Models.Orchestrations.RecordingPipeline.Exceptions;

public class FailedRecordingPipelineDependencyException : Xeption
{
    public FailedRecordingPipelineDependencyException(Exception innerException)
        : base("Failed recording pipeline dependency error occurred, contact support.", innerException)
    { }
}
