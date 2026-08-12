using System.Net;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;
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
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode == HttpStatusCode.BadRequest)
        {
            var invalidQueueConfigException = new InvalidQueueConfigException();
            throw await CreateAndLogDependencyValidationException(invalidQueueConfigException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode == HttpStatusCode.Conflict)
        {
            var alreadyExistsQueueConfigException = new AlreadyExistsQueueConfigException(httpRequestException);
            throw await CreateAndLogDependencyValidationException(alreadyExistsQueueConfigException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or null)
        {
            var failedAsteriskQueueConfigDependencyException =
                new FailedAsteriskQueueConfigDependencyException(httpRequestException);

            throw await CreateAndLogCriticalDependencyException(failedAsteriskQueueConfigDependencyException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode is HttpStatusCode.InternalServerError
                or HttpStatusCode.ServiceUnavailable)
        {
            var failedAsteriskQueueConfigDependencyException =
                new FailedAsteriskQueueConfigDependencyException(httpRequestException);

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
