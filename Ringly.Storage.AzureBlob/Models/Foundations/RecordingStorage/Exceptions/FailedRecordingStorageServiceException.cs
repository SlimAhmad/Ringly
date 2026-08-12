using Xeptions;

namespace Ringly.Storage.AzureBlob.Models.Foundations.RecordingStorage.Exceptions;

public class FailedRecordingStorageServiceException : Xeption
{
    public FailedRecordingStorageServiceException(Exception innerException)
        : base("Failed recording storage service error occurred, contact support.", innerException)
    { }
}
