using System.Net;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;
using Xeptions;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
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
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode == HttpStatusCode.BadRequest)
        {
            var invalidCallParticipantException = new InvalidCallParticipantException();
            throw await CreateAndLogDependencyValidationException(invalidCallParticipantException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or null)
        {
            var failedAsteriskCallProviderDependencyException =
                new FailedAsteriskCallProviderDependencyException(httpRequestException);

            throw await CreateAndLogCriticalDependencyException(failedAsteriskCallProviderDependencyException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode is HttpStatusCode.InternalServerError
                or HttpStatusCode.ServiceUnavailable)
        {
            var failedAsteriskCallProviderDependencyException =
                new FailedAsteriskCallProviderDependencyException(httpRequestException);

            throw await CreateAndLogDependencyException(failedAsteriskCallProviderDependencyException);
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
