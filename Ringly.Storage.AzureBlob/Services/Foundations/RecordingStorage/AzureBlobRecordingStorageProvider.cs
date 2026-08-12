using Ringly.Storage.Abstractions;
using Ringly.Storage.AzureBlob.Brokers;

namespace Ringly.Storage.AzureBlob.Services.Foundations.RecordingStorage;

public partial class AzureBlobRecordingStorageProvider : IRecordingStorageProvider
{
    private readonly IAzureBlobBroker azureBlobBroker;
    private readonly ILoggingBroker loggingBroker;

    public AzureBlobRecordingStorageProvider(
        IAzureBlobBroker azureBlobBroker,
        ILoggingBroker loggingBroker)
    {
        this.azureBlobBroker = azureBlobBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<Uri> UploadRecordingAsync(string localFilePath, string recordingId) =>
    TryCatch(async () =>
    {
        ValidateRecordingId(recordingId);
        ValidateLocalFilePath(localFilePath);

        return await this.azureBlobBroker.UploadAsync(recordingId, localFilePath);
    });

    public ValueTask<Stream> DownloadRecordingAsync(string recordingId) =>
    TryCatch(async () =>
    {
        ValidateRecordingId(recordingId);

        return await this.azureBlobBroker.DownloadAsync(recordingId);
    });

    public ValueTask DeleteRecordingAsync(string recordingId) =>
    TryCatch(async () =>
    {
        ValidateRecordingId(recordingId);

        await this.azureBlobBroker.DeleteAsync(recordingId);
    });

    public ValueTask<Uri> GenerateTemporaryAccessUrlAsync(string recordingId, TimeSpan expiry) =>
    TryCatch(async () =>
    {
        ValidateRecordingId(recordingId);

        return await this.azureBlobBroker.GenerateSasUriAsync(recordingId, expiry);
    });
}
