using Xeptions;

namespace Ringly.Asterisk.Models.Orchestrations.RecordingPipeline.Exceptions;

public class InvalidRecordingPipelineRequestException : Xeption
{
    public InvalidRecordingPipelineRequestException()
        : base("Recording pipeline request is invalid.")
    { }
}
