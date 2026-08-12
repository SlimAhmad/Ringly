namespace Ringly.Storage.AzureBlob.Brokers;

public interface IAzureBlobBroker
{
    ValueTask<Uri> UploadAsync(string recordingId, string localFilePath);
    ValueTask<Stream> DownloadAsync(string recordingId);
    ValueTask DeleteAsync(string recordingId);
    ValueTask<Uri> GenerateSasUriAsync(string recordingId, TimeSpan expiry);
}
