using Xeptions;

namespace Ringly.Asterisk.Models.Orchestrations.Onboarding.Exceptions;

public class FailedUserOnboardingServiceException : Xeption
{
    public FailedUserOnboardingServiceException(Exception innerException)
        : base("Failed user onboarding service error occurred, contact support.", innerException)
    { }
}
