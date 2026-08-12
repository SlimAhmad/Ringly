namespace Ringly.Asterisk.Services.Orchestrations.RecordingPipeline;

public interface IRecordingOrchestrationService
{
    ValueTask<Uri> UploadFinishedRecordingAsync(string recordingName, string format);
}
