using EFxceptions.Models.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;
using Xeptions;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityService
{
    private delegate ValueTask<TelephonyIdentity> ReturningTelephonyIdentityFunction();
    private delegate ValueTask<TelephonyIdentity?> ReturningNullableTelephonyIdentityFunction();
    private delegate ValueTask<IQueryable<TelephonyIdentity>> ReturningTelephonyIdentitiesFunction();

    private async ValueTask<TelephonyIdentity> TryCatch(
        ReturningTelephonyIdentityFunction returningTelephonyIdentityFunction)
    {
        try
        {
            return await returningTelephonyIdentityFunction();
        }
        catch (NullTelephonyIdentityException nullTelephonyIdentityException)
        {
            throw await CreateAndLogValidationExceptionAsync(nullTelephonyIdentityException);
        }
        catch (InvalidTelephonyIdentityException invalidTelephonyIdentityException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidTelephonyIdentityException);
        }
        catch (NotFoundTelephonyIdentityException notFoundTelephonyIdentityException)
        {
            throw await CreateAndLogValidationExceptionAsync(notFoundTelephonyIdentityException);
        }
        catch (SqlException sqlException)
        {
            var failedStorageTelephonyIdentityDependencyException =
                new FailedStorageTelephonyIdentityDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedStorageTelephonyIdentityDependencyException);
        }
        catch (DuplicateKeyException duplicateKeyException)
        {
            var alreadyExistsTelephonyIdentityException =
                new AlreadyExistsTelephonyIdentityException(duplicateKeyException);

            throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsTelephonyIdentityException);
        }
        catch (DbUpdateException dbUpdateException)
        {
            var failedStorageTelephonyIdentityDependencyException =
                new FailedStorageTelephonyIdentityDependencyException(dbUpdateException);

            throw await CreateAndLogDependencyExceptionAsync(failedStorageTelephonyIdentityDependencyException);
        }
        catch (Exception exception)
        {
            var failedTelephonyIdentityServiceException = new FailedTelephonyIdentityServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedTelephonyIdentityServiceException);
        }
    }

    private async ValueTask<TelephonyIdentity?> TryCatchNullable(
        ReturningNullableTelephonyIdentityFunction returningNullableTelephonyIdentityFunction)
    {
        try
        {
            return await returningNullableTelephonyIdentityFunction();
        }
        catch (InvalidTelephonyIdentityException invalidTelephonyIdentityException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidTelephonyIdentityException);
        }
        catch (SqlException sqlException)
        {
            var failedStorageTelephonyIdentityDependencyException =
                new FailedStorageTelephonyIdentityDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedStorageTelephonyIdentityDependencyException);
        }
        catch (Exception exception)
        {
            var failedTelephonyIdentityServiceException = new FailedTelephonyIdentityServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedTelephonyIdentityServiceException);
        }
    }

    private async ValueTask<IQueryable<TelephonyIdentity>> TryCatch(
        ReturningTelephonyIdentitiesFunction returningTelephonyIdentitiesFunction)
    {
        try
        {
            return await returningTelephonyIdentitiesFunction();
        }
        catch (SqlException sqlException)
        {
            var failedStorageTelephonyIdentityDependencyException =
                new FailedStorageTelephonyIdentityDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedStorageTelephonyIdentityDependencyException);
        }
        catch (Exception exception)
        {
            var failedTelephonyIdentityServiceException = new FailedTelephonyIdentityServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedTelephonyIdentityServiceException);
        }
    }

    private async ValueTask<TelephonyIdentityValidationException> CreateAndLogValidationExceptionAsync(
        Xeption exception)
    {
        var telephonyIdentityValidationException = new TelephonyIdentityValidationException(exception);
        await this.loggingBroker.LogErrorAsync(telephonyIdentityValidationException);

        return telephonyIdentityValidationException;
    }

    private async ValueTask<TelephonyIdentityDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Xeption exception)
    {
        var telephonyIdentityDependencyException = new TelephonyIdentityDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(telephonyIdentityDependencyException);

        return telephonyIdentityDependencyException;
    }

    private async ValueTask<TelephonyIdentityDependencyValidationException>
        CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
    {
        var telephonyIdentityDependencyValidationException =
            new TelephonyIdentityDependencyValidationException(exception);

        await this.loggingBroker.LogErrorAsync(telephonyIdentityDependencyValidationException);

        return telephonyIdentityDependencyValidationException;
    }

    private async ValueTask<TelephonyIdentityDependencyException> CreateAndLogDependencyExceptionAsync(
        Xeption exception)
    {
        var telephonyIdentityDependencyException = new TelephonyIdentityDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(telephonyIdentityDependencyException);

        return telephonyIdentityDependencyException;
    }

    private async ValueTask<TelephonyIdentityServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
    {
        var telephonyIdentityServiceException = new TelephonyIdentityServiceException(exception);
        await this.loggingBroker.LogErrorAsync(telephonyIdentityServiceException);

        return telephonyIdentityServiceException;
    }
}
