using System.Net;
using Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;
using Xeptions;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService
{
    private delegate ValueTask ReturningValueTaskFunction();

    private async ValueTask TryCatchTransfer(ReturningValueTaskFunction returningValueTaskFunction)
    {
        try
        {
            await returningValueTaskFunction();
        }
        catch (InvalidTransferProgressRequestException invalidTransferProgressRequestException)
        {
            throw await CreateAndLogTransferValidationException(invalidTransferProgressRequestException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode == HttpStatusCode.BadRequest)
        {
            var invalidTransferProgressRequestException = new InvalidTransferProgressRequestException();
            throw await CreateAndLogTransferDependencyValidationException(invalidTransferProgressRequestException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or null)
        {
            var failedAsteriskTransferDependencyException =
                new FailedAsteriskTransferDependencyException(httpRequestException);

            throw await CreateAndLogTransferCriticalDependencyException(failedAsteriskTransferDependencyException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode is HttpStatusCode.InternalServerError
                or HttpStatusCode.ServiceUnavailable)
        {
            var failedAsteriskTransferDependencyException =
                new FailedAsteriskTransferDependencyException(httpRequestException);

            throw await CreateAndLogTransferDependencyException(failedAsteriskTransferDependencyException);
        }
        catch (Exception exception)
        {
            var failedTransferServiceException = new FailedTransferServiceException(exception);
            throw await CreateAndLogTransferServiceException(failedTransferServiceException);
        }
    }

    private async ValueTask<TransferValidationException> CreateAndLogTransferValidationException(Xeption exception)
    {
        var transferValidationException = new TransferValidationException(exception);
        await this.loggingBroker.LogErrorAsync(transferValidationException);

        return transferValidationException;
    }

    private async ValueTask<TransferDependencyValidationException> CreateAndLogTransferDependencyValidationException(
        Xeption exception)
    {
        var transferDependencyValidationException = new TransferDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(transferDependencyValidationException);

        return transferDependencyValidationException;
    }

    private async ValueTask<TransferDependencyException> CreateAndLogTransferCriticalDependencyException(
        Xeption exception)
    {
        var transferDependencyException = new TransferDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(transferDependencyException);

        return transferDependencyException;
    }

    private async ValueTask<TransferDependencyException> CreateAndLogTransferDependencyException(Xeption exception)
    {
        var transferDependencyException = new TransferDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(transferDependencyException);

        return transferDependencyException;
    }

    private async ValueTask<TransferServiceException> CreateAndLogTransferServiceException(Xeption exception)
    {
        var transferServiceException = new TransferServiceException(exception);
        await this.loggingBroker.LogErrorAsync(transferServiceException);

        return transferServiceException;
    }
}
