using Xeptions;

namespace Ringly.Storage.AzureBlob.Models.Foundations.RecordingStorage.Exceptions;

public class RecordingStorageValidationException : Xeption
{
    public RecordingStorageValidationException(Xeption innerException)
        : base("Recording storage validation error occurred, fix errors and try again.", innerException)
    { }
}
