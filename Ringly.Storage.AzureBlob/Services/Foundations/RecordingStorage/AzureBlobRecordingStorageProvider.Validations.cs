using Ringly.Storage.AzureBlob.Models.Foundations.RecordingStorage.Exceptions;

namespace Ringly.Storage.AzureBlob.Services.Foundations.RecordingStorage;

public partial class AzureBlobRecordingStorageProvider
{
    private static void ValidateRecordingId(string recordingId)
    {
        if (string.IsNullOrWhiteSpace(recordingId))
        {
            var invalidRecordingStorageRequestException = new InvalidRecordingStorageRequestException();

            invalidRecordingStorageRequestException.UpsertDataList(
                key: nameof(recordingId),
                value: "Value is required");

            invalidRecordingStorageRequestException.ThrowIfContainsErrors();
        }
    }

    private static void ValidateLocalFilePath(string localFilePath)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            var invalidRecordingStorageRequestException = new InvalidRecordingStorageRequestException();

            invalidRecordingStorageRequestException.UpsertDataList(
                key: nameof(localFilePath),
                value: "Value is required");

            invalidRecordingStorageRequestException.ThrowIfContainsErrors();
        }
    }
}
