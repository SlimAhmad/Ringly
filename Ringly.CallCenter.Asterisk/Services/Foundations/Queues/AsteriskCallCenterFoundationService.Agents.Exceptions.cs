using Ringly.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;
using RESTFulSense.Exceptions;
using Xeptions;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService
{
    private delegate ValueTask<ClaimResult> ReturningClaimResultFunction();
    private delegate ValueTask AgentReturningValueTaskFunction();

    private async ValueTask<ClaimResult> TryCatchClaim(ReturningClaimResultFunction returningClaimResultFunction)
    {
        try
        {
            return await returningClaimResultFunction();
        }
        catch (InvalidAgentRequestException invalidAgentRequestException)
        {
            throw await CreateAndLogAgentValidationException(invalidAgentRequestException);
        }
        catch (HttpResponseBadRequestException)
        {
            var invalidAgentRequestException = new InvalidAgentRequestException();
            throw await CreateAndLogAgentDependencyValidationException(invalidAgentRequestException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogAgentCriticalDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogAgentCriticalDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseNotFoundException);

            throw await CreateAndLogAgentCriticalDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpRequestException);

            throw await CreateAndLogAgentCriticalDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogAgentDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogAgentDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseException);

            throw await CreateAndLogAgentDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (Exception exception)
        {
            var failedAgentServiceException = new FailedAgentServiceException(exception);
            throw await CreateAndLogAgentServiceException(failedAgentServiceException);
        }
    }

    private async ValueTask TryCatchAgent(AgentReturningValueTaskFunction returningValueTaskFunction)
    {
        try
        {
            await returningValueTaskFunction();
        }
        catch (InvalidAgentRequestException invalidAgentRequestException)
        {
            throw await CreateAndLogAgentValidationException(invalidAgentRequestException);
        }
        catch (HttpResponseBadRequestException)
        {
            var invalidAgentRequestException = new InvalidAgentRequestException();
            throw await CreateAndLogAgentDependencyValidationException(invalidAgentRequestException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogAgentCriticalDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogAgentCriticalDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseNotFoundException);

            throw await CreateAndLogAgentCriticalDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpRequestException);

            throw await CreateAndLogAgentCriticalDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogAgentDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogAgentDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedAsteriskAgentDependencyException =
                new FailedAsteriskAgentDependencyException(httpResponseException);

            throw await CreateAndLogAgentDependencyException(failedAsteriskAgentDependencyException);
        }
        catch (Exception exception)
        {
            var failedAgentServiceException = new FailedAgentServiceException(exception);
            throw await CreateAndLogAgentServiceException(failedAgentServiceException);
        }
    }

    private async ValueTask<AgentValidationException> CreateAndLogAgentValidationException(Xeption exception)
    {
        var agentValidationException = new AgentValidationException(exception);
        await this.loggingBroker.LogErrorAsync(agentValidationException);

        return agentValidationException;
    }

    private async ValueTask<AgentDependencyValidationException> CreateAndLogAgentDependencyValidationException(
        Xeption exception)
    {
        var agentDependencyValidationException = new AgentDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(agentDependencyValidationException);

        return agentDependencyValidationException;
    }

    private async ValueTask<AgentDependencyException> CreateAndLogAgentCriticalDependencyException(
        Xeption exception)
    {
        var agentDependencyException = new AgentDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(agentDependencyException);

        return agentDependencyException;
    }

    private async ValueTask<AgentDependencyException> CreateAndLogAgentDependencyException(Xeption exception)
    {
        var agentDependencyException = new AgentDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(agentDependencyException);

        return agentDependencyException;
    }

    private async ValueTask<AgentServiceException> CreateAndLogAgentServiceException(Xeption exception)
    {
        var agentServiceException = new AgentServiceException(exception);
        await this.loggingBroker.LogErrorAsync(agentServiceException);

        return agentServiceException;
    }
}
