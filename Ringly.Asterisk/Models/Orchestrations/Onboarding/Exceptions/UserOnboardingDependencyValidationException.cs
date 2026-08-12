using Xeptions;

namespace Ringly.Asterisk.Models.Orchestrations.Onboarding.Exceptions;

public class UserOnboardingDependencyValidationException : Xeption
{
    public UserOnboardingDependencyValidationException(Xeption innerException)
        : base("User onboarding dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
