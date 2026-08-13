using Ringly.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;
using RESTFulSense.Exceptions;
using Xeptions;

namespace Ringly.Trunking.Asterisk.Services.Foundations.Trunks;

public partial class SipTrunkFoundationService
{
    private const string DependencyErrorMessage = "SIP trunk dependency error occurred, contact support.";

    private delegate ValueTask<Channel> ReturningChannelFunction();

    private async ValueTask<Channel> TryCatch(ReturningChannelFunction returningChannelFunction)
    {
        try
        {
            return await returningChannelFunction();
        }
        catch (InvalidDialOutRequestException invalidDialOutRequestException)
        {
            throw await CreateAndLogValidationException(invalidDialOutRequestException);
        }
        catch (BlockedDestinationException blockedDestinationException)
        {
            throw await CreateAndLogValidationException(blockedDestinationException);
        }
        catch (TrunkSpendLimitExceededException trunkSpendLimitExceededException)
        {
            throw await CreateAndLogDependencyValidationException(trunkSpendLimitExceededException);
        }
        catch (HttpResponseBadRequestException)
        {
            throw await CreateAndLogDependencyValidationException(new InvalidDialOutRequestException());
        }
        catch (HttpResponseConflictException)
        {
            throw await CreateAndLogDependencyValidationException(new InvalidDialOutRequestException());
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            throw await CreateAndLogCriticalDependencyException(httpResponseNotFoundException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            throw await CreateAndLogCriticalDependencyException(httpResponseUnauthorizedException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            throw await CreateAndLogCriticalDependencyException(httpResponseForbiddenException);
        }
        catch (HttpRequestException httpRequestException)
        {
            throw await CreateAndLogCriticalDependencyException(httpRequestException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            throw await CreateAndLogDependencyException(httpResponseInternalServerErrorException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            throw await CreateAndLogDependencyException(httpResponseServiceUnavailableException);
        }
        catch (HttpResponseException httpResponseException)
        {
            throw await CreateAndLogDependencyException(httpResponseException);
        }
        catch (Exception exception)
        {
            var failedSipTrunkServiceException = new FailedSipTrunkServiceException(exception);
            throw await CreateAndLogServiceException(failedSipTrunkServiceException);
        }
    }

    private async ValueTask<SipTrunkValidationException> CreateAndLogValidationException(Xeption exception)
    {
        var sipTrunkValidationException = new SipTrunkValidationException(exception);
        await this.loggingBroker.LogErrorAsync(sipTrunkValidationException);

        return sipTrunkValidationException;
    }

    private async ValueTask<SipTrunkDependencyValidationException> CreateAndLogDependencyValidationException(
        Xeption exception)
    {
        var sipTrunkDependencyValidationException = new SipTrunkDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(sipTrunkDependencyValidationException);

        return sipTrunkDependencyValidationException;
    }

    private async ValueTask<SipTrunkDependencyException> CreateAndLogCriticalDependencyException(Exception exception)
    {
        var sipTrunkDependencyException = new SipTrunkDependencyException(DependencyErrorMessage, exception);
        await this.loggingBroker.LogCriticalAsync(sipTrunkDependencyException);

        return sipTrunkDependencyException;
    }

    private async ValueTask<SipTrunkDependencyException> CreateAndLogDependencyException(Exception exception)
    {
        var sipTrunkDependencyException = new SipTrunkDependencyException(DependencyErrorMessage, exception);
        await this.loggingBroker.LogErrorAsync(sipTrunkDependencyException);

        return sipTrunkDependencyException;
    }

    private async ValueTask<SipTrunkServiceException> CreateAndLogServiceException(Xeption exception)
    {
        var sipTrunkServiceException = new SipTrunkServiceException(exception);
        await this.loggingBroker.LogErrorAsync(sipTrunkServiceException);

        return sipTrunkServiceException;
    }
}
