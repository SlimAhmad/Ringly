namespace Ringly.Storage.AzureBlob.Brokers;

public interface IAzureBlobBroker
{
    ValueTask<Uri> UploadAsync(string recordingId, string localFilePath);
    ValueTask<Stream> DownloadAsync(string recordingId);
    ValueTask DeleteAsync(string recordingId);
    ValueTask<Uri> GenerateSasUriAsync(string recordingId, TimeSpan expiry);

    // A one-time infra-setup step (call once at app startup), not part of the per-recording
    // upload/download/delete contract above — the container isn't created implicitly on first
    // use, an upload against a missing container just 404s.
    ValueTask EnsureContainerExistsAsync();
}
