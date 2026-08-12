using Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;
using RESTFulSense.Exceptions;
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
        catch (HttpResponseBadRequestException)
        {
            var invalidTransferProgressRequestException = new InvalidTransferProgressRequestException();
            throw await CreateAndLogTransferDependencyValidationException(invalidTransferProgressRequestException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            var failedAsteriskTransferDependencyException =
                new FailedAsteriskTransferDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogTransferCriticalDependencyException(failedAsteriskTransferDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedAsteriskTransferDependencyException =
                new FailedAsteriskTransferDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogTransferCriticalDependencyException(failedAsteriskTransferDependencyException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var failedAsteriskTransferDependencyException =
                new FailedAsteriskTransferDependencyException(httpResponseNotFoundException);

            throw await CreateAndLogTransferCriticalDependencyException(failedAsteriskTransferDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedAsteriskTransferDependencyException =
                new FailedAsteriskTransferDependencyException(httpRequestException);

            throw await CreateAndLogTransferCriticalDependencyException(failedAsteriskTransferDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedAsteriskTransferDependencyException =
                new FailedAsteriskTransferDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogTransferDependencyException(failedAsteriskTransferDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedAsteriskTransferDependencyException =
                new FailedAsteriskTransferDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogTransferDependencyException(failedAsteriskTransferDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedAsteriskTransferDependencyException =
                new FailedAsteriskTransferDependencyException(httpResponseException);

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
