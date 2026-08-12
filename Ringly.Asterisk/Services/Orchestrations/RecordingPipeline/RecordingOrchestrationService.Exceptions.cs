using Ringly.Asterisk.Models.Orchestrations.RecordingPipeline.Exceptions;
using Xeptions;

namespace Ringly.Asterisk.Services.Orchestrations.RecordingPipeline;

public partial class RecordingOrchestrationService
{
    private delegate ValueTask<Uri> ReturningUriFunction();

    private async ValueTask<Uri> TryCatch(ReturningUriFunction returningUriFunction)
    {
        try
        {
            return await returningUriFunction();
        }
        catch (InvalidRecordingPipelineRequestException invalidRecordingPipelineRequestException)
        {
            throw await CreateAndLogValidationException(invalidRecordingPipelineRequestException);
        }
        catch (Exception exception)
        {
            var failedRecordingPipelineDependencyException =
                new FailedRecordingPipelineDependencyException(exception);

            throw await CreateAndLogDependencyException(failedRecordingPipelineDependencyException);
        }
    }

    private async ValueTask<RecordingPipelineValidationException> CreateAndLogValidationException(Xeption exception)
    {
        var recordingPipelineValidationException = new RecordingPipelineValidationException(exception);
        await this.loggingBroker.LogErrorAsync(recordingPipelineValidationException);

        return recordingPipelineValidationException;
    }

    private async ValueTask<RecordingPipelineDependencyException> CreateAndLogDependencyException(Xeption exception)
    {
        var recordingPipelineDependencyException = new RecordingPipelineDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(recordingPipelineDependencyException);

        return recordingPipelineDependencyException;
    }
}
