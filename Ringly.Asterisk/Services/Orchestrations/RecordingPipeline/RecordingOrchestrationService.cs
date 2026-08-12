using Microsoft.Extensions.Options;
using Ringly.Asterisk.Brokers;
using Ringly.Storage.Abstractions;

namespace Ringly.Asterisk.Services.Orchestrations.RecordingPipeline;

public partial class RecordingOrchestrationService : IRecordingOrchestrationService
{
    private readonly IRecordingStorageProvider recordingStorageProvider;
    private readonly ILoggingBroker loggingBroker;
    private readonly RecordingPipelineOptions options;

    public RecordingOrchestrationService(
        IRecordingStorageProvider recordingStorageProvider,
        ILoggingBroker loggingBroker,
        IOptions<RecordingPipelineOptions> options)
    {
        this.recordingStorageProvider = recordingStorageProvider;
        this.loggingBroker = loggingBroker;
        this.options = options.Value;
    }

    public ValueTask<Uri> UploadFinishedRecordingAsync(string recordingName, string format) =>
    TryCatch(async () =>
    {
        ValidateRecordingName(recordingName);
        ValidateFormat(format);

        string localFilePath = Path.Combine(this.options.RecordingsSpoolPath, $"{recordingName}.{format}");

        return await this.recordingStorageProvider.UploadRecordingAsync(localFilePath, recordingName);
    });
}
