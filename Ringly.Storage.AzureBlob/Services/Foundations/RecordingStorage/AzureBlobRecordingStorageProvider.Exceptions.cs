using Azure;
using Ringly.Storage.AzureBlob.Models.Foundations.RecordingStorage.Exceptions;
using Xeptions;

namespace Ringly.Storage.AzureBlob.Services.Foundations.RecordingStorage;

public partial class AzureBlobRecordingStorageProvider
{
    private delegate ValueTask<T> ReturningFunction<T>();
    private delegate ValueTask ReturningValueTaskFunction();

    private async ValueTask<T> TryCatch<T>(ReturningFunction<T> returningFunction)
    {
        try
        {
            return await returningFunction();
        }
        catch (InvalidRecordingStorageRequestException invalidRecordingStorageRequestException)
        {
            throw await CreateAndLogValidationException(invalidRecordingStorageRequestException);
        }
        catch (RequestFailedException requestFailedException)
        {
            var failedAzureBlobDependencyException = new FailedAzureBlobDependencyException(requestFailedException);
            throw await CreateAndLogDependencyException(failedAzureBlobDependencyException);
        }
        catch (Exception exception)
        {
            var failedRecordingStorageServiceException = new FailedRecordingStorageServiceException(exception);
            throw await CreateAndLogServiceException(failedRecordingStorageServiceException);
        }
    }

    private async ValueTask TryCatch(ReturningValueTaskFunction returningValueTaskFunction)
    {
        try
        {
            await returningValueTaskFunction();
        }
        catch (InvalidRecordingStorageRequestException invalidRecordingStorageRequestException)
        {
            throw await CreateAndLogValidationException(invalidRecordingStorageRequestException);
        }
        catch (RequestFailedException requestFailedException)
        {
            var failedAzureBlobDependencyException = new FailedAzureBlobDependencyException(requestFailedException);
            throw await CreateAndLogDependencyException(failedAzureBlobDependencyException);
        }
        catch (Exception exception)
        {
            var failedRecordingStorageServiceException = new FailedRecordingStorageServiceException(exception);
            throw await CreateAndLogServiceException(failedRecordingStorageServiceException);
        }
    }

    private async ValueTask<RecordingStorageValidationException> CreateAndLogValidationException(Xeption exception)
    {
        var recordingStorageValidationException = new RecordingStorageValidationException(exception);
        await this.loggingBroker.LogErrorAsync(recordingStorageValidationException);

        return recordingStorageValidationException;
    }

    private async ValueTask<RecordingStorageDependencyException> CreateAndLogDependencyException(Xeption exception)
    {
        var recordingStorageDependencyException = new RecordingStorageDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(recordingStorageDependencyException);

        return recordingStorageDependencyException;
    }

    private async ValueTask<RecordingStorageServiceException> CreateAndLogServiceException(Xeption exception)
    {
        var recordingStorageServiceException = new RecordingStorageServiceException(exception);
        await this.loggingBroker.LogErrorAsync(recordingStorageServiceException);

        return recordingStorageServiceException;
    }
}
