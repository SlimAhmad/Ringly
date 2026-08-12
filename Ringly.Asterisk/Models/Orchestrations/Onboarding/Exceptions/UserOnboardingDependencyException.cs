using Xeptions;

namespace Ringly.Asterisk.Models.Orchestrations.Onboarding.Exceptions;

public class UserOnboardingDependencyException : Xeption
{
    public UserOnboardingDependencyException(Xeption innerException)
        : base("User onboarding dependency error occurred, contact support.", innerException)
    { }
}
