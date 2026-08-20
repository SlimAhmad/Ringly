using EFxceptions.Models.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ringly.Samples.WebApi.Models.Foundations.SupportQueues;
using Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;
using Xeptions;

namespace Ringly.Samples.WebApi.Services.Foundations.SupportQueues;

public partial class SupportQueueService
{
    private delegate ValueTask<SupportQueue> ReturningSupportQueueFunction();
    private delegate ValueTask<SupportQueue?> ReturningNullableSupportQueueFunction();
    private delegate ValueTask<IQueryable<SupportQueue>> ReturningSupportQueuesFunction();

    private async ValueTask<SupportQueue> TryCatch(
        ReturningSupportQueueFunction returningSupportQueueFunction)
    {
        try
        {
            return await returningSupportQueueFunction();
        }
        catch (NullSupportQueueException nullSupportQueueException)
        {
            throw await CreateAndLogValidationExceptionAsync(nullSupportQueueException);
        }
        catch (InvalidSupportQueueException invalidSupportQueueException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidSupportQueueException);
        }
        catch (NotFoundSupportQueueException notFoundSupportQueueException)
        {
            throw await CreateAndLogValidationExceptionAsync(notFoundSupportQueueException);
        }
        catch (SqlException sqlException)
        {
            var failedStorageSupportQueueDependencyException =
                new FailedStorageSupportQueueDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedStorageSupportQueueDependencyException);
        }
        catch (DuplicateKeyException duplicateKeyException)
        {
            var alreadyExistsSupportQueueException =
                new AlreadyExistsSupportQueueException(duplicateKeyException);

            throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsSupportQueueException);
        }
        catch (DbUpdateException dbUpdateException)
        {
            var failedStorageSupportQueueDependencyException =
                new FailedStorageSupportQueueDependencyException(dbUpdateException);

            throw await CreateAndLogDependencyExceptionAsync(failedStorageSupportQueueDependencyException);
        }
        catch (Exception exception)
        {
            var failedSupportQueueServiceException = new FailedSupportQueueServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedSupportQueueServiceException);
        }
    }

    private async ValueTask<SupportQueue?> TryCatchNullable(
        ReturningNullableSupportQueueFunction returningNullableSupportQueueFunction)
    {
        try
        {
            return await returningNullableSupportQueueFunction();
        }
        catch (InvalidSupportQueueException invalidSupportQueueException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidSupportQueueException);
        }
        catch (SqlException sqlException)
        {
            var failedStorageSupportQueueDependencyException =
                new FailedStorageSupportQueueDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedStorageSupportQueueDependencyException);
        }
        catch (Exception exception)
        {
            var failedSupportQueueServiceException = new FailedSupportQueueServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedSupportQueueServiceException);
        }
    }

    private async ValueTask<IQueryable<SupportQueue>> TryCatch(
        ReturningSupportQueuesFunction returningSupportQueuesFunction)
    {
        try
        {
            return await returningSupportQueuesFunction();
        }
        catch (SqlException sqlException)
        {
            var failedStorageSupportQueueDependencyException =
                new FailedStorageSupportQueueDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedStorageSupportQueueDependencyException);
        }
        catch (Exception exception)
        {
            var failedSupportQueueServiceException = new FailedSupportQueueServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedSupportQueueServiceException);
        }
    }

    private async ValueTask<SupportQueueValidationException> CreateAndLogValidationExceptionAsync(
        Xeption exception)
    {
        var supportQueueValidationException = new SupportQueueValidationException(exception);
        await this.loggingBroker.LogErrorAsync(supportQueueValidationException);

        return supportQueueValidationException;
    }

    private async ValueTask<SupportQueueDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Xeption exception)
    {
        var supportQueueDependencyException = new SupportQueueDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(supportQueueDependencyException);

        return supportQueueDependencyException;
    }

    private async ValueTask<SupportQueueDependencyValidationException>
        CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
    {
        var supportQueueDependencyValidationException =
            new SupportQueueDependencyValidationException(exception);

        await this.loggingBroker.LogErrorAsync(supportQueueDependencyValidationException);

        return supportQueueDependencyValidationException;
    }

    private async ValueTask<SupportQueueDependencyException> CreateAndLogDependencyExceptionAsync(
        Xeption exception)
    {
        var supportQueueDependencyException = new SupportQueueDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(supportQueueDependencyException);

        return supportQueueDependencyException;
    }

    private async ValueTask<SupportQueueServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
    {
        var supportQueueServiceException = new SupportQueueServiceException(exception);
        await this.loggingBroker.LogErrorAsync(supportQueueServiceException);

        return supportQueueServiceException;
    }
}
