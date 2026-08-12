using Xeptions;

namespace Ringly.Asterisk.Models.Orchestrations.RecordingPipeline.Exceptions;

public class RecordingPipelineValidationException : Xeption
{
    public RecordingPipelineValidationException(Xeption innerException)
        : base("Recording pipeline validation error occurred, fix errors and try again.", innerException)
    { }
}
