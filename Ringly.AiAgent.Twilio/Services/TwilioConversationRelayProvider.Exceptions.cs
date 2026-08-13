using Ringly.AiAgent.Abstractions.Models;
using Ringly.AiAgent.Twilio.Models.Exceptions;
using RESTFulSense.Exceptions;
using Xeptions;

namespace Ringly.AiAgent.Twilio.Services;

public partial class TwilioConversationRelayProvider
{
    private delegate ValueTask<AiAgentSession> ReturningAiAgentSessionFunction();
    private delegate ValueTask ReturningValueTaskFunction();

    private async ValueTask<AiAgentSession> TryCatch(ReturningAiAgentSessionFunction returningAiAgentSessionFunction)
    {
        try
        {
            return await returningAiAgentSessionFunction();
        }
        catch (NullAiAgentConfigException nullAiAgentConfigException)
        {
            throw await CreateAndLogValidationException(nullAiAgentConfigException);
        }
        catch (InvalidAiAgentSessionRequestException invalidAiAgentSessionRequestException)
        {
            throw await CreateAndLogValidationException(invalidAiAgentSessionRequestException);
        }
        catch (HttpResponseBadRequestException)
        {
            var invalidAiAgentSessionRequestException = new InvalidAiAgentSessionRequestException();
            throw await CreateAndLogDependencyValidationException(invalidAiAgentSessionRequestException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            var failedTwilioAiAgentDependencyException =
                new FailedTwilioAiAgentDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioAiAgentDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedTwilioAiAgentDependencyException =
                new FailedTwilioAiAgentDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioAiAgentDependencyException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var failedTwilioAiAgentDependencyException =
                new FailedTwilioAiAgentDependencyException(httpResponseNotFoundException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioAiAgentDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedTwilioAiAgentDependencyException =
                new FailedTwilioAiAgentDependencyException(httpRequestException);

            throw await CreateAndLogCriticalDependencyException(failedTwilioAiAgentDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedTwilioAiAgentDependencyException =
                new FailedTwilioAiAgentDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogDependencyException(failedTwilioAiAgentDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedTwilioAiAgentDependencyException =
                new FailedTwilioAiAgentDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogDependencyException(failedTwilioAiAgentDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedTwilioAiAgentDependencyException =
                new FailedTwilioAiAgentDependencyException(httpResponseException);

            throw await CreateAndLogDependencyException(failedTwilioAiAgentDependencyException);
        }
        catch (Exception exception)
        {
            var failedAiAgentServiceException = new FailedAiAgentServiceException(exception);
            throw await CreateAndLogServiceException(failedAiAgentServiceException);
        }
    }

    private async ValueTask TryCatchSession(ReturningValueTaskFunction returningValueTaskFunction)
    {
        try
        {
            await returningValueTaskFunction();
        }
        catch (AiAgentSessionNotFoundException aiAgentSessionNotFoundException)
        {
            throw await CreateAndLogValidationException(aiAgentSessionNotFoundException);
        }
        catch (InvalidAiAgentSessionRequestException invalidAiAgentSessionRequestException)
        {
            throw await CreateAndLogValidationException(invalidAiAgentSessionRequestException);
        }
        catch (Exception exception)
        {
            var failedAiAgentServiceException = new FailedAiAgentServiceException(exception);
            throw await CreateAndLogServiceException(failedAiAgentServiceException);
        }
    }

    private async ValueTask<AiAgentSessionValidationException> CreateAndLogValidationException(Xeption exception)
    {
        var aiAgentSessionValidationException = new AiAgentSessionValidationException(exception);
        await this.loggingBroker.LogErrorAsync(aiAgentSessionValidationException);

        return aiAgentSessionValidationException;
    }

    private async ValueTask<AiAgentSessionDependencyValidationException> CreateAndLogDependencyValidationException(
        Xeption exception)
    {
        var aiAgentSessionDependencyValidationException = new AiAgentSessionDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(aiAgentSessionDependencyValidationException);

        return aiAgentSessionDependencyValidationException;
    }

    private async ValueTask<AiAgentSessionDependencyException> CreateAndLogCriticalDependencyException(
        Xeption exception)
    {
        var aiAgentSessionDependencyException = new AiAgentSessionDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(aiAgentSessionDependencyException);

        return aiAgentSessionDependencyException;
    }

    private async ValueTask<AiAgentSessionDependencyException> CreateAndLogDependencyException(Xeption exception)
    {
        var aiAgentSessionDependencyException = new AiAgentSessionDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(aiAgentSessionDependencyException);

        return aiAgentSessionDependencyException;
    }

    private async ValueTask<AiAgentSessionServiceException> CreateAndLogServiceException(Xeption exception)
    {
        var aiAgentSessionServiceException = new AiAgentSessionServiceException(exception);
        await this.loggingBroker.LogErrorAsync(aiAgentSessionServiceException);

        return aiAgentSessionServiceException;
    }
}
