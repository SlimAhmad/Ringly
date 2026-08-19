using EFxceptions.Models.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;
using Xeptions;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallService
{
    private delegate ValueTask<TelephonyCall> ReturningTelephonyCallFunction();
    private delegate ValueTask<IQueryable<TelephonyCall>> ReturningTelephonyCallsFunction();

    private async ValueTask<TelephonyCall> TryCatch(ReturningTelephonyCallFunction returningTelephonyCallFunction)
    {
        try
        {
            return await returningTelephonyCallFunction();
        }
        catch (NullTelephonyCallException nullTelephonyCallException)
        {
            throw await CreateAndLogValidationExceptionAsync(nullTelephonyCallException);
        }
        catch (InvalidTelephonyCallException invalidTelephonyCallException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidTelephonyCallException);
        }
        catch (NotFoundTelephonyCallException notFoundTelephonyCallException)
        {
            throw await CreateAndLogValidationExceptionAsync(notFoundTelephonyCallException);
        }
        catch (SqlException sqlException)
        {
            var failedStorageTelephonyCallDependencyException =
                new FailedStorageTelephonyCallDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedStorageTelephonyCallDependencyException);
        }
        catch (DuplicateKeyException duplicateKeyException)
        {
            var alreadyExistsTelephonyCallException =
                new AlreadyExistsTelephonyCallException(duplicateKeyException);

            throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsTelephonyCallException);
        }
        catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
        {
            var invalidReferenceTelephonyCallException =
                new InvalidReferenceTelephonyCallException(foreignKeyConstraintConflictException);

            throw await CreateAndLogDependencyValidationExceptionAsync(invalidReferenceTelephonyCallException);
        }
        catch (DbUpdateException dbUpdateException)
        {
            var failedStorageTelephonyCallDependencyException =
                new FailedStorageTelephonyCallDependencyException(dbUpdateException);

            throw await CreateAndLogDependencyExceptionAsync(failedStorageTelephonyCallDependencyException);
        }
        catch (Exception exception)
        {
            var failedTelephonyCallServiceException = new FailedTelephonyCallServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedTelephonyCallServiceException);
        }
    }

    private async ValueTask<IQueryable<TelephonyCall>> TryCatch(
        ReturningTelephonyCallsFunction returningTelephonyCallsFunction)
    {
        try
        {
            return await returningTelephonyCallsFunction();
        }
        catch (InvalidTelephonyCallException invalidTelephonyCallException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidTelephonyCallException);
        }
        catch (SqlException sqlException)
        {
            var failedStorageTelephonyCallDependencyException =
                new FailedStorageTelephonyCallDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedStorageTelephonyCallDependencyException);
        }
        catch (Exception exception)
        {
            var failedTelephonyCallServiceException = new FailedTelephonyCallServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedTelephonyCallServiceException);
        }
    }

    private async ValueTask<TelephonyCallValidationException> CreateAndLogValidationExceptionAsync(
        Xeption exception)
    {
        var telephonyCallValidationException = new TelephonyCallValidationException(exception);
        await this.loggingBroker.LogErrorAsync(telephonyCallValidationException);

        return telephonyCallValidationException;
    }

    private async ValueTask<TelephonyCallDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Xeption exception)
    {
        var telephonyCallDependencyException = new TelephonyCallDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(telephonyCallDependencyException);

        return telephonyCallDependencyException;
    }

    private async ValueTask<TelephonyCallDependencyValidationException>
        CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
    {
        var telephonyCallDependencyValidationException = new TelephonyCallDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(telephonyCallDependencyValidationException);

        return telephonyCallDependencyValidationException;
    }

    private async ValueTask<TelephonyCallDependencyException> CreateAndLogDependencyExceptionAsync(
        Xeption exception)
    {
        var telephonyCallDependencyException = new TelephonyCallDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(telephonyCallDependencyException);

        return telephonyCallDependencyException;
    }

    private async ValueTask<TelephonyCallServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
    {
        var telephonyCallServiceException = new TelephonyCallServiceException(exception);
        await this.loggingBroker.LogErrorAsync(telephonyCallServiceException);

        return telephonyCallServiceException;
    }
}
