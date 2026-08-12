namespace Ringly.Storage.Abstractions;

public interface IRecordingStorageProvider
{
    ValueTask<Uri> UploadRecordingAsync(string localFilePath, string recordingId);
    ValueTask<Stream> DownloadRecordingAsync(string recordingId);
    ValueTask DeleteRecordingAsync(string recordingId);
    ValueTask<Uri> GenerateTemporaryAccessUrlAsync(string recordingId, TimeSpan expiry);
}
