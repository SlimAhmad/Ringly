using Ringly.Abstractions.Models;
using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;
using RESTFulSense.Exceptions;
using Xeptions;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

public partial class TwilioCallProvider
{
    private delegate ValueTask<CallSession> ReturningCallSessionFunction();

    private async ValueTask<CallSession> TryCatch(ReturningCallSessionFunction returningCallSessionFunction)
    {
        try
        {
            return await returningCallSessionFunction();
        }
        catch (NullCallParticipantException nullCallParticipantException)
        {
            throw await CreateAndLogValidationException(nullCallParticipantException);
        }
        catch (InvalidCallParticipantException invalidCallParticipantException)
        {
            throw await CreateAndLogValidationException(invalidCallParticipantException);
        }
        catch (InvalidRouteToQueueRequestException invalidRouteToQueueRequestException)
        {
            throw await CreateAndLogValidationException(invalidRouteToQueueRequestException);
        }
        catch (NotFoundSipCredentialsException notFoundSipCredentialsException)
        {
            throw await CreateAndLogValidationException(notFoundSipCredentialsException);
        }
        catch (HttpResponseBadRequestException)
        {
            var invalidCallParticipantException = new InvalidCallParticipantException();
            throw await CreateAndLogDependencyValidationException(invalidCallParticipantException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseNotFoundException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpRequestException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseException);

            throw await CreateAndLogDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (Exception exception)
        {
            var failedCallProviderServiceException = new FailedCallProviderServiceException(exception);
            throw await CreateAndLogServiceException(failedCallProviderServiceException);
        }
    }

    private delegate ValueTask<Channel> ReturningChannelFunction();

    // Separate TryCatch overload (not the CallSession-returning one above) since
    // ConnectAgentToQueueAsync returns Channel — same catch ladder and CreateAndLog* helpers,
    // just one extra validation-exception catch for this routine's own request-shape check.
    private async ValueTask<Channel> TryCatchChannel(ReturningChannelFunction returningChannelFunction)
    {
        try
        {
            return await returningChannelFunction();
        }
        catch (InvalidConnectAgentToQueueRequestException invalidConnectAgentToQueueRequestException)
        {
            throw await CreateAndLogValidationException(invalidConnectAgentToQueueRequestException);
        }
        catch (HttpResponseBadRequestException)
        {
            var invalidCallParticipantException = new InvalidCallParticipantException();
            throw await CreateAndLogDependencyValidationException(invalidCallParticipantException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseNotFoundException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpRequestException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedTwilioCallProviderDependencyException =
                new FailedTwilioCallProviderDependencyException(httpResponseException);

            throw await CreateAndLogDependencyException(failedTwilioCallProviderDependencyException);
        }
        catch (Exception exception)
        {
            var failedCallProviderServiceException = new FailedCallProviderServiceException(exception);
            throw await CreateAndLogServiceException(failedCallProviderServiceException);
        }
    }

    private async ValueTask<CallSessionValidationException> CreateAndLogValidationException(Xeption exception)
    {
        var callSessionValidationException = new CallSessionValidationException(exception);
        await this.loggingBroker.LogErrorAsync(callSessionValidationException);

        return callSessionValidationException;
    }

    private async ValueTask<CallSessionDependencyValidationException> CreateAndLogDependencyValidationException(
        Xeption exception)
    {
        var callSessionDependencyValidationException = new CallSessionDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(callSessionDependencyValidationException);

        return callSessionDependencyValidationException;
    }

    private async ValueTask<CallProviderDependencyException> CreateAndLogCriticalDependencyException(
        Xeption exception)
    {
        var callProviderDependencyException = new CallProviderDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(callProviderDependencyException);

        return callProviderDependencyException;
    }

    private async ValueTask<CallProviderDependencyException> CreateAndLogDependencyException(Xeption exception)
    {
        var callProviderDependencyException = new CallProviderDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(callProviderDependencyException);

        return callProviderDependencyException;
    }

    private async ValueTask<CallProviderServiceException> CreateAndLogServiceException(Xeption exception)
    {
        var callProviderServiceException = new CallProviderServiceException(exception);
        await this.loggingBroker.LogErrorAsync(callProviderServiceException);

        return callProviderServiceException;
    }
}
