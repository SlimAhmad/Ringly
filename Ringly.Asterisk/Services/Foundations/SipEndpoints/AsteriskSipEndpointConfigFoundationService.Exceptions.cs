using System.Net;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;
using Xeptions;

namespace Ringly.Asterisk.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationService
{
    private delegate ValueTask ReturningValueTaskFunction();

    private async ValueTask TryCatch(ReturningValueTaskFunction returningValueTaskFunction)
    {
        try
        {
            await returningValueTaskFunction();
        }
        catch (NullSipEndpointConfigException nullSipEndpointConfigException)
        {
            throw await CreateAndLogValidationException(nullSipEndpointConfigException);
        }
        catch (InvalidSipEndpointConfigException invalidSipEndpointConfigException)
        {
            throw await CreateAndLogValidationException(invalidSipEndpointConfigException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode == HttpStatusCode.BadRequest)
        {
            var invalidSipEndpointConfigException = new InvalidSipEndpointConfigException();
            throw await CreateAndLogDependencyValidationException(invalidSipEndpointConfigException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode == HttpStatusCode.Conflict)
        {
            var duplicateExtensionException = new DuplicateExtensionException(httpRequestException);
            throw await CreateAndLogDependencyValidationException(duplicateExtensionException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode == HttpStatusCode.NotFound)
        {
            var notFoundSipEndpointConfigException = new NotFoundSipEndpointConfigException(httpRequestException);

            throw await CreateAndLogCriticalDependencyException(
                new FailedAsteriskSipEndpointConfigDependencyException(notFoundSipEndpointConfigException));
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or null)
        {
            var failedAsteriskSipEndpointConfigDependencyException =
                new FailedAsteriskSipEndpointConfigDependencyException(httpRequestException);

            throw await CreateAndLogCriticalDependencyException(failedAsteriskSipEndpointConfigDependencyException);
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode is HttpStatusCode.InternalServerError
                or HttpStatusCode.ServiceUnavailable)
        {
            var failedAsteriskSipEndpointConfigDependencyException =
                new FailedAsteriskSipEndpointConfigDependencyException(httpRequestException);

            throw await CreateAndLogDependencyException(failedAsteriskSipEndpointConfigDependencyException);
        }
        catch (Exception exception)
        {
            var failedSipEndpointConfigServiceException = new FailedSipEndpointConfigServiceException(exception);
            throw await CreateAndLogServiceException(failedSipEndpointConfigServiceException);
        }
    }

    private async ValueTask<SipEndpointConfigValidationException> CreateAndLogValidationException(Xeption exception)
    {
        var sipEndpointConfigValidationException = new SipEndpointConfigValidationException(exception);
        await this.loggingBroker.LogErrorAsync(sipEndpointConfigValidationException);

        return sipEndpointConfigValidationException;
    }

    private async ValueTask<SipEndpointConfigDependencyValidationException> CreateAndLogDependencyValidationException(
        Xeption exception)
    {
        var sipEndpointConfigDependencyValidationException =
            new SipEndpointConfigDependencyValidationException(exception);

        await this.loggingBroker.LogErrorAsync(sipEndpointConfigDependencyValidationException);

        return sipEndpointConfigDependencyValidationException;
    }

    private async ValueTask<SipEndpointConfigDependencyException> CreateAndLogCriticalDependencyException(
        Xeption exception)
    {
        var sipEndpointConfigDependencyException = new SipEndpointConfigDependencyException(exception);
        await this.loggingBroker.LogCriticalAsync(sipEndpointConfigDependencyException);

        return sipEndpointConfigDependencyException;
    }

    private async ValueTask<SipEndpointConfigDependencyException> CreateAndLogDependencyException(Xeption exception)
    {
        var sipEndpointConfigDependencyException = new SipEndpointConfigDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(sipEndpointConfigDependencyException);

        return sipEndpointConfigDependencyException;
    }

    private async ValueTask<SipEndpointConfigServiceException> CreateAndLogServiceException(Xeption exception)
    {
        var sipEndpointConfigServiceException = new SipEndpointConfigServiceException(exception);
        await this.loggingBroker.LogErrorAsync(sipEndpointConfigServiceException);

        return sipEndpointConfigServiceException;
    }
}
