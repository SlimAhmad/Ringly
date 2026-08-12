using Ringly.Asterisk.Models.Orchestrations.RecordingPipeline.Exceptions;

namespace Ringly.Asterisk.Services.Orchestrations.RecordingPipeline;

public partial class RecordingOrchestrationService
{
    private static void ValidateRecordingName(string recordingName)
    {
        if (string.IsNullOrWhiteSpace(recordingName))
        {
            var invalidRecordingPipelineRequestException = new InvalidRecordingPipelineRequestException();

            invalidRecordingPipelineRequestException.UpsertDataList(
                key: nameof(recordingName),
                value: "Value is required");

            invalidRecordingPipelineRequestException.ThrowIfContainsErrors();
        }
    }

    private static void ValidateFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            var invalidRecordingPipelineRequestException = new InvalidRecordingPipelineRequestException();

            invalidRecordingPipelineRequestException.UpsertDataList(
                key: nameof(format),
                value: "Value is required");

            invalidRecordingPipelineRequestException.ThrowIfContainsErrors();
        }
    }
}
