using EFxceptions.Models.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;
using Xeptions;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceService
{
    private delegate ValueTask<TelephonyDevice> ReturningTelephonyDeviceFunction();
    private delegate ValueTask<IQueryable<TelephonyDevice>> ReturningTelephonyDevicesFunction();

    private async ValueTask<TelephonyDevice> TryCatch(
        ReturningTelephonyDeviceFunction returningTelephonyDeviceFunction)
    {
        try
        {
            return await returningTelephonyDeviceFunction();
        }
        catch (NullTelephonyDeviceException nullTelephonyDeviceException)
        {
            throw await CreateAndLogValidationExceptionAsync(nullTelephonyDeviceException);
        }
        catch (InvalidTelephonyDeviceException invalidTelephonyDeviceException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidTelephonyDeviceException);
        }
        catch (NotFoundTelephonyDeviceException notFoundTelephonyDeviceException)
        {
            throw await CreateAndLogValidationExceptionAsync(notFoundTelephonyDeviceException);
        }
        catch (SqlException sqlException)
        {
            var failedStorageTelephonyDeviceDependencyException =
                new FailedStorageTelephonyDeviceDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedStorageTelephonyDeviceDependencyException);
        }
        catch (DuplicateKeyException duplicateKeyException)
        {
            var alreadyExistsTelephonyDeviceException =
                new AlreadyExistsTelephonyDeviceException(duplicateKeyException);

            throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsTelephonyDeviceException);
        }
        catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
        {
            var invalidReferenceTelephonyDeviceException =
                new InvalidReferenceTelephonyDeviceException(foreignKeyConstraintConflictException);

            throw await CreateAndLogDependencyValidationExceptionAsync(invalidReferenceTelephonyDeviceException);
        }
        catch (DbUpdateException dbUpdateException)
        {
            var failedStorageTelephonyDeviceDependencyException =
                new FailedStorageTelephonyDeviceDependencyException(dbUpdateException);

            throw await CreateAndLogDependencyExceptionAsync(failedStorageTelephonyDeviceDependencyException);
        }
        catch (Exception exception)
        {
            var failedTelephonyDeviceServiceException = new FailedTelephonyDeviceServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedTelephonyDeviceServiceException);
        }
    }

    private async ValueTask<IQueryable<TelephonyDevice>> TryCatch(
        ReturningTelephonyDevicesFunction returningTelephonyDevicesFunction)
    {
        try
        {
            return await returningTelephonyDevicesFunction();
        }
        catch (InvalidTelephonyDeviceException invalidTelephonyDeviceException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidTelephonyDeviceException);
        }
        catch (SqlException sqlException)
        {
            var failedStorageTelephonyDeviceDependencyException =
                new FailedStorageTelephonyDeviceDependencyException(sqlException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedStorageTelephonyDeviceDependencyException);
        }
        catch (Exception exception)
        {
            var failedTelephonyDeviceServiceException = new FailedTelephonyDeviceServiceException(exception);

            throw await CreateAndLogServiceExceptionAsync(failedTelephonyDeviceServiceException);
        }
    }

    private async ValueTask<TelephonyDeviceValidationException> CreateAndLogValidationExceptionAsync(
        Xeption exception)
    {
        var telephonyDeviceValidationException = new TelephonyDeviceValidationException(exception);
        await this.loggingBroker.LogErrorAsync(telephonyDeviceValidationException);

        return telephonyDeviceValidationException;
    }

    private async ValueTask<TelephonyDeviceDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Xeption exception)
    {
        var telephonyDeviceDependencyException = new TelephonyDeviceDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(telephonyDeviceDependencyException);

        return telephonyDeviceDependencyException;
    }

    private async ValueTask<TelephonyDeviceDependencyValidationException>
        CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
    {
        var telephonyDeviceDependencyValidationException =
            new TelephonyDeviceDependencyValidationException(exception);

        await this.loggingBroker.LogErrorAsync(telephonyDeviceDependencyValidationException);

        return telephonyDeviceDependencyValidationException;
    }

    private async ValueTask<TelephonyDeviceDependencyException> CreateAndLogDependencyExceptionAsync(
        Xeption exception)
    {
        var telephonyDeviceDependencyException = new TelephonyDeviceDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(telephonyDeviceDependencyException);

        return telephonyDeviceDependencyException;
    }

    private async ValueTask<TelephonyDeviceServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
    {
        var telephonyDeviceServiceException = new TelephonyDeviceServiceException(exception);
        await this.loggingBroker.LogErrorAsync(telephonyDeviceServiceException);

        return telephonyDeviceServiceException;
    }
}
