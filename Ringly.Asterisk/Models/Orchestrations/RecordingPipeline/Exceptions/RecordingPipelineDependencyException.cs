using Xeptions;

namespace Ringly.Asterisk.Models.Orchestrations.RecordingPipeline.Exceptions;

public class RecordingPipelineDependencyException : Xeption
{
    public RecordingPipelineDependencyException(Xeption innerException)
        : base("Recording pipeline dependency error occurred, contact support.", innerException)
    { }
}
