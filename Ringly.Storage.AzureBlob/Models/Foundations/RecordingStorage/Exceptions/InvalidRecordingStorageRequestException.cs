using Xeptions;

namespace Ringly.Storage.AzureBlob.Models.Foundations.RecordingStorage.Exceptions;

public class InvalidRecordingStorageRequestException : Xeption
{
    public InvalidRecordingStorageRequestException()
        : base("Recording storage request is invalid.")
    { }
}
