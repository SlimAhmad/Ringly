using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;
using RESTFulSense.Exceptions;
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
        catch (DuplicateExtensionException duplicateExtensionException)
        {
            throw await CreateAndLogDependencyValidationException(duplicateExtensionException);
        }
        catch (HttpResponseBadRequestException)
        {
            var invalidSipEndpointConfigException = new InvalidSipEndpointConfigException();
            throw await CreateAndLogDependencyValidationException(invalidSipEndpointConfigException);
        }
        catch (HttpResponseConflictException httpResponseConflictException)
        {
            var duplicateExtensionException = new DuplicateExtensionException(httpResponseConflictException);
            throw await CreateAndLogDependencyValidationException(duplicateExtensionException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            var notFoundSipEndpointConfigException = new NotFoundSipEndpointConfigException(httpResponseNotFoundException);

            throw await CreateAndLogCriticalDependencyException(
                new FailedAsteriskSipEndpointConfigDependencyException(notFoundSipEndpointConfigException));
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            var failedAsteriskSipEndpointConfigDependencyException =
                new FailedAsteriskSipEndpointConfigDependencyException(httpResponseUnauthorizedException);

            throw await CreateAndLogCriticalDependencyException(failedAsteriskSipEndpointConfigDependencyException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            var failedAsteriskSipEndpointConfigDependencyException =
                new FailedAsteriskSipEndpointConfigDependencyException(httpResponseForbiddenException);

            throw await CreateAndLogCriticalDependencyException(failedAsteriskSipEndpointConfigDependencyException);
        }
        catch (HttpRequestException httpRequestException)
        {
            var failedAsteriskSipEndpointConfigDependencyException =
                new FailedAsteriskSipEndpointConfigDependencyException(httpRequestException);

            throw await CreateAndLogCriticalDependencyException(failedAsteriskSipEndpointConfigDependencyException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            var failedAsteriskSipEndpointConfigDependencyException =
                new FailedAsteriskSipEndpointConfigDependencyException(httpResponseInternalServerErrorException);

            throw await CreateAndLogDependencyException(failedAsteriskSipEndpointConfigDependencyException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            var failedAsteriskSipEndpointConfigDependencyException =
                new FailedAsteriskSipEndpointConfigDependencyException(httpResponseServiceUnavailableException);

            throw await CreateAndLogDependencyException(failedAsteriskSipEndpointConfigDependencyException);
        }
        catch (HttpResponseException httpResponseException)
        {
            var failedAsteriskSipEndpointConfigDependencyException =
                new FailedAsteriskSipEndpointConfigDependencyException(httpResponseException);

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
