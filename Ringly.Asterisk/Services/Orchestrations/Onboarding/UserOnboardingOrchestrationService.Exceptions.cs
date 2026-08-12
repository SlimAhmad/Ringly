using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Orchestrations.Onboarding.Exceptions;
using Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;
using Xeptions;

namespace Ringly.Asterisk.Services.Orchestrations.Onboarding;

public partial class UserOnboardingOrchestrationService
{
    private delegate ValueTask<SipCredentials> ReturningSipCredentialsFunction();

    private async ValueTask<SipCredentials> TryCatch(ReturningSipCredentialsFunction returningSipCredentialsFunction)
    {
        try
        {
            return await returningSipCredentialsFunction();
        }
        catch (SipCredentialsValidationException sipCredentialsValidationException)
        {
            throw await CreateAndLogValidationException(sipCredentialsValidationException);
        }
        catch (SipCredentialsDependencyValidationException sipCredentialsDependencyValidationException)
        {
            throw await CreateAndLogDependencyValidationException(sipCredentialsDependencyValidationException);
        }
        catch (SipCredentialsDependencyException sipCredentialsDependencyException)
        {
            throw await CreateAndLogDependencyException(sipCredentialsDependencyException);
        }
        catch (SipCredentialsServiceException sipCredentialsServiceException)
        {
            throw await CreateAndLogServiceException(sipCredentialsServiceException);
        }
        catch (Exception exception)
        {
            var failedUserOnboardingServiceException = new FailedUserOnboardingServiceException(exception);
            throw await CreateAndLogServiceException(failedUserOnboardingServiceException);
        }
    }

    private async ValueTask<UserOnboardingValidationException> CreateAndLogValidationException(Xeption exception)
    {
        var userOnboardingValidationException = new UserOnboardingValidationException(exception);
        await this.loggingBroker.LogErrorAsync(userOnboardingValidationException);

        return userOnboardingValidationException;
    }

    private async ValueTask<UserOnboardingDependencyValidationException> CreateAndLogDependencyValidationException(
        Xeption exception)
    {
        var userOnboardingDependencyValidationException = new UserOnboardingDependencyValidationException(exception);
        await this.loggingBroker.LogErrorAsync(userOnboardingDependencyValidationException);

        return userOnboardingDependencyValidationException;
    }

    private async ValueTask<UserOnboardingDependencyException> CreateAndLogDependencyException(Xeption exception)
    {
        var userOnboardingDependencyException = new UserOnboardingDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(userOnboardingDependencyException);

        return userOnboardingDependencyException;
    }

    private async ValueTask<UserOnboardingServiceException> CreateAndLogServiceException(Xeption exception)
    {
        var userOnboardingServiceException = new UserOnboardingServiceException(exception);
        await this.loggingBroker.LogErrorAsync(userOnboardingServiceException);

        return userOnboardingServiceException;
    }
}
