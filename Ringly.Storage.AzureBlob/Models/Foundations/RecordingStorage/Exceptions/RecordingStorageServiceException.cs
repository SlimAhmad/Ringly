using Xeptions;

namespace Ringly.Storage.AzureBlob.Models.Foundations.RecordingStorage.Exceptions;

public class RecordingStorageServiceException : Xeption
{
    public RecordingStorageServiceException(Xeption innerException)
        : base("Recording storage service error occurred, contact support.", innerException)
    { }
}
