using Xeptions;

namespace Ringly.Storage.AzureBlob.Models.Foundations.RecordingStorage.Exceptions;

public class RecordingStorageDependencyException : Xeption
{
    public RecordingStorageDependencyException(Xeption innerException)
        : base("Recording storage dependency error occurred, contact support.", innerException)
    { }
}
