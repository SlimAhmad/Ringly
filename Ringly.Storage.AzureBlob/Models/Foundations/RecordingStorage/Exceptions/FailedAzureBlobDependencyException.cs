using Xeptions;

namespace Ringly.Storage.AzureBlob.Models.Foundations.RecordingStorage.Exceptions;

public class FailedAzureBlobDependencyException : Xeption
{
    public FailedAzureBlobDependencyException(Exception innerException)
        : base("Failed Azure Blob dependency error occurred, contact support.", innerException)
    { }
}
