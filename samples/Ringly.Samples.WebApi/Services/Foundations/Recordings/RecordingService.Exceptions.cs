using EFxceptions.Models.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ringly.Samples.WebApi.Models.Foundations.Recordings;
using Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;
using Xeptions;

namespace Ringly.Samples.WebApi.Services.Foundations.Recordings;

public partial class RecordingService
{
    private delegate ValueTask<Recording> ReturningRecordingFunction();
    private delegate ValueTask<Recording?> ReturningNullableRecordingFunction();
    private delegate ValueTask<IQueryable<Recording>> ReturningRecordingsFunction();

    private async ValueTask<Recording> TryCatch(ReturningRecordingFunction returningRecordingFunction)
    {
        try
        {
            return await returningRecordingFunction();
        }
        catch (NullRecordingException nullRecordingException)
        {
            throw await CreateAndLogValidationExceptionAsync(nullRecordingException);
        }
        catch (InvalidRecordingException invalidRecordingException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidRecordingException);
        }
        catch (NotFoundRecordingException notFoundRecordingException)
        {
            throw await CreateAndLogValidationExceptionAsync(notFoundRecordingException);
        }
        catch (SqlException sqlException)
        {
            var failedStorageRecordingDependencyException =
                new FailedStorageRecordingDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(failedStorageRecordingDependencyException);
        }
        catch (DuplicateKeyException duplicateKeyException)
        {
            var alreadyExistsRecordingException = new AlreadyExistsRecordingException(duplicateKeyException);

            throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsRecordingException);
        }
        catch (DbUpdateException dbUpdateException)
        {
            var failedStorageRecordingDependencyException =
                new FailedStorageRecordingDependencyException(dbUpdateException);

            throw await CreateAndLogDependencyExceptionAsync(failedStorageRecordingDependencyException);
        }
        catch (Exception exception)
        {
            var failedRecordingServiceException = new FailedRecordingServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedRecordingServiceException);
        }
    }

    private async ValueTask<Recording?> TryCatchNullable(
        ReturningNullableRecordingFunction returningNullableRecordingFunction)
    {
        try
        {
            return await returningNullableRecordingFunction();
        }
        catch (InvalidRecordingException invalidRecordingException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidRecordingException);
        }
        catch (SqlException sqlException)
        {
            var failedStorageRecordingDependencyException =
                new FailedStorageRecordingDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(failedStorageRecordingDependencyException);
        }
        catch (Exception exception)
        {
            var failedRecordingServiceException = new FailedRecordingServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedRecordingServiceException);
        }
    }

    private async ValueTask<IQueryable<Recording>> TryCatch(
        ReturningRecordingsFunction returningRecordingsFunction)
    {
        try
        {
            return await returningRecordingsFunction();
        }
        catch (SqlException sqlException)
        {
            var failedStorageRecordingDependencyException =
                new FailedStorageRecordingDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(failedStorageRecordingDependencyException);
        }
        catch (Exception exception)
        {
            var failedRecordingServiceException = new FailedRecordingServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedRecordingServiceException);
        }
    }

    private async ValueTask<RecordingValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
    {
        var recordingValidationException = new RecordingValidationException(exception);
        await this.loggingBroker.LogErrorAsync(recordingValidationException);

        return recordingValidationException;
    }

    private async ValueTask<RecordingDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Xeption exception)
    {
        var recordingDependencyException = new RecordingDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(recordingDependencyException);

        return recordingDependencyException;
    }

    private async ValueTask<RecordingDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
        Xeption exception)
    {
        var recordingDependencyValidationException = new RecordingDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(recordingDependencyValidationException);

        return recordingDependencyValidationException;
    }

    private async ValueTask<RecordingDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
    {
        var recordingDependencyException = new RecordingDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(recordingDependencyException);

        return recordingDependencyException;
    }

    private async ValueTask<RecordingServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
    {
        var recordingServiceException = new RecordingServiceException(exception);
        await this.loggingBroker.LogErrorAsync(recordingServiceException);

        return recordingServiceException;
    }
}
