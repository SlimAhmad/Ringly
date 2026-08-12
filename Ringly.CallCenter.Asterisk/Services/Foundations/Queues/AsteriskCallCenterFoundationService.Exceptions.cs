using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;
using RESTFulSense.Exceptions;
using Xeptions;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService
{
    private delegate ValueTask<HoldingBridge> ReturningHoldingBridgeFunction();

    private async ValueTask<HoldingBridge> TryCatch(ReturningHoldingBridgeFunction returningHoldingBridgeFunction)
    {
        try
        {
            return await returningHoldingBridgeFunction();
        }
        catch (NullQueueConfigException nullQueueConfigException)
        {
            throw await CreateAndLogValidationException(nullQueueConfigException);
        }
        catch (InvalidQueueConfigException invalidQueueConfigException)
        {
            throw await CreateAndLogValidationException(invalidQueueConfigException);
        }
        catch (HttpResponseBadRequestException)
        {
            var invalidQueueConfigException = new InvalidQueueConfigException();
            throw await CreateAndLogDependencyValidationException(invalidQueueConfigException);
        }
        catch (HttpResponseConflictException httpResponseConflictException)
        {
            var alreadyExistsQueueConfigException = new AlreadyExistsQueueConfigException(httpResponseConflictException);
            throw await CreateAndLogDependencyValidationException(alreadyExistsQueueConfigException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            var failedAsteriskQueueConfigDependencyException =
                new FailedAsteriskQueueConfigDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogCriticalDependencyException(failedAsteriskQueueConfigDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedAsteriskQueueConfigDependencyException =
                new FailedAsteriskQueueConfigDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogCriticalDependencyException(failedAsteriskQueueConfigDependencyException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var failedAsteriskQueueConfigDependencyException =
                new FailedAsteriskQueueConfigDependencyException(httpResponseNotFoundException);

            throw await CreateAndLogCriticalDependencyException(failedAsteriskQueueConfigDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedAsteriskQueueConfigDependencyException =
                new FailedAsteriskQueueConfigDependencyException(httpRequestException);

            throw await CreateAndLogCriticalDependencyException(failedAsteriskQueueConfigDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedAsteriskQueueConfigDependencyException =
                new FailedAsteriskQueueConfigDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogDependencyException(failedAsteriskQueueConfigDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedAsteriskQueueConfigDependencyException =
                new FailedAsteriskQueueConfigDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogDependencyException(failedAsteriskQueueConfigDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedAsteriskQueueConfigDependencyException =
                new FailedAsteriskQueueConfigDependencyException(httpResponseException);

            throw await CreateAndLogDependencyException(failedAsteriskQueueConfigDependencyException);
        }
        catch (Exception exception)
        {
            var failedQueueConfigServiceException = new FailedQueueConfigServiceException(exception);
            throw await CreateAndLogServiceException(failedQueueConfigServiceException);
        }
    }

    private async ValueTask<QueueConfigValidationException> CreateAndLogValidationException(Xeption exception)
    {
        var queueConfigValidationException = new QueueConfigValidationException(exception);
        await this.loggingBroker.LogErrorAsync(queueConfigValidationException);

        return queueConfigValidationException;
    }

    private async ValueTask<QueueConfigDependencyValidationException> CreateAndLogDependencyValidationException(
        Xeption exception)
    {
        var queueConfigDependencyValidationException = new QueueConfigDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(queueConfigDependencyValidationException);

        return queueConfigDependencyValidationException;
    }

    private async ValueTask<QueueConfigDependencyException> CreateAndLogCriticalDependencyException(
        Xeption exception)
    {
        var queueConfigDependencyException = new QueueConfigDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(queueConfigDependencyException);

        return queueConfigDependencyException;
    }

    private async ValueTask<QueueConfigDependencyException> CreateAndLogDependencyException(Xeption exception)
    {
        var queueConfigDependencyException = new QueueConfigDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(queueConfigDependencyException);

        return queueConfigDependencyException;
    }

    private async ValueTask<QueueConfigServiceException> CreateAndLogServiceException(Xeption exception)
    {
        var queueConfigServiceException = new QueueConfigServiceException(exception);
        await this.loggingBroker.LogErrorAsync(queueConfigServiceException);

        return queueConfigServiceException;
    }
}
