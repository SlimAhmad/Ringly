using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Twilio.Models.Foundations.Queues.Exceptions;
using RESTFulSense.Exceptions;
using Xeptions;

namespace Ringly.CallCenter.Twilio.Services.Foundations.Queues;

public partial class TwilioCallCenterProvider
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
            var failedTwilioQueueConfigDependencyException =
                new FailedTwilioQueueConfigDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioQueueConfigDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedTwilioQueueConfigDependencyException =
                new FailedTwilioQueueConfigDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioQueueConfigDependencyException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var failedTwilioQueueConfigDependencyException =
                new FailedTwilioQueueConfigDependencyException(httpResponseNotFoundException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioQueueConfigDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedTwilioQueueConfigDependencyException =
                new FailedTwilioQueueConfigDependencyException(httpRequestException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioQueueConfigDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedTwilioQueueConfigDependencyException =
                new FailedTwilioQueueConfigDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogDependencyException(failedTwilioQueueConfigDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedTwilioQueueConfigDependencyException =
                new FailedTwilioQueueConfigDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogDependencyException(failedTwilioQueueConfigDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedTwilioQueueConfigDependencyException =
                new FailedTwilioQueueConfigDependencyException(httpResponseException);

            throw await CreateAndLogDependencyException(failedTwilioQueueConfigDependencyException);
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
