using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;
using Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;
using Xeptions;

namespace Ringly.Asterisk.Services.Processings.Provisioning;

public partial class CallProvisioningService
{
    private delegate ValueTask<SipCredentials> ReturningSipCredentialsFunction();

    private async ValueTask<SipCredentials> TryCatch(ReturningSipCredentialsFunction returningSipCredentialsFunction)
    {
        try
        {
            return await returningSipCredentialsFunction();
        }
        catch (InvalidSipCredentialsException invalidSipCredentialsException)
        {
            throw await CreateAndLogValidationException(invalidSipCredentialsException);
        }
        catch (SipEndpointConfigValidationException sipEndpointConfigValidationException)
        {
            throw await CreateAndLogValidationException(sipEndpointConfigValidationException);
        }
        catch (SipEndpointConfigDependencyValidationException sipEndpointConfigDependencyValidationException)
        {
            throw await CreateAndLogDependencyValidationException(sipEndpointConfigDependencyValidationException);
        }
        catch (SipEndpointConfigDependencyException sipEndpointConfigDependencyException)
        {
            throw await CreateAndLogDependencyException(sipEndpointConfigDependencyException);
        }
        catch (SipEndpointConfigServiceException sipEndpointConfigServiceException)
        {
            throw await CreateAndLogServiceException(sipEndpointConfigServiceException);
        }
        catch (Exception exception)
        {
            var failedSipCredentialsServiceException = new FailedSipCredentialsServiceException(exception);
            throw await CreateAndLogServiceException(failedSipCredentialsServiceException);
        }
    }

    private delegate ValueTask ReturningValueTaskFunction();

    private async ValueTask TryCatch(ReturningValueTaskFunction returningValueTaskFunction)
    {
        try
        {
            await returningValueTaskFunction();
        }
        catch (InvalidSipCredentialsException invalidSipCredentialsException)
        {
            throw await CreateAndLogValidationException(invalidSipCredentialsException);
        }
        catch (SipEndpointConfigValidationException sipEndpointConfigValidationException)
        {
            throw await CreateAndLogValidationException(sipEndpointConfigValidationException);
        }
        catch (SipEndpointConfigDependencyValidationException sipEndpointConfigDependencyValidationException)
        {
            throw await CreateAndLogDependencyValidationException(sipEndpointConfigDependencyValidationException);
        }
        catch (SipEndpointConfigDependencyException sipEndpointConfigDependencyException)
        {
            throw await CreateAndLogDependencyException(sipEndpointConfigDependencyException);
        }
        catch (SipEndpointConfigServiceException sipEndpointConfigServiceException)
        {
            throw await CreateAndLogServiceException(sipEndpointConfigServiceException);
        }
        catch (Exception exception)
        {
            var failedSipCredentialsServiceException = new FailedSipCredentialsServiceException(exception);
            throw await CreateAndLogServiceException(failedSipCredentialsServiceException);
        }
    }

    private async ValueTask<SipCredentialsValidationException> CreateAndLogValidationException(Xeption exception)
    {
        var sipCredentialsValidationException = new SipCredentialsValidationException(exception);
        await this.loggingBroker.LogErrorAsync(sipCredentialsValidationException);

        return sipCredentialsValidationException;
    }

    private async ValueTask<SipCredentialsDependencyValidationException> CreateAndLogDependencyValidationException(
        Xeption exception)
    {
        var sipCredentialsDependencyValidationException = new SipCredentialsDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(sipCredentialsDependencyValidationException);

        return sipCredentialsDependencyValidationException;
    }

    private async ValueTask<SipCredentialsDependencyException> CreateAndLogDependencyException(Xeption exception)
    {
        var sipCredentialsDependencyException = new SipCredentialsDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(sipCredentialsDependencyException);

        return sipCredentialsDependencyException;
    }

    private async ValueTask<SipCredentialsServiceException> CreateAndLogServiceException(Xeption exception)
    {
        var sipCredentialsServiceException = new SipCredentialsServiceException(exception);
        await this.loggingBroker.LogErrorAsync(sipCredentialsServiceException);

        return sipCredentialsServiceException;
    }
}
