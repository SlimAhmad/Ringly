using Xeptions;

namespace Ringly.Asterisk.Models.Orchestrations.Onboarding.Exceptions;

public class UserOnboardingServiceException : Xeption
{
    public UserOnboardingServiceException(Xeption innerException)
        : base("User onboarding service error occurred, contact support.", innerException)
    { }
}
