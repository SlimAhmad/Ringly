using Xeptions;

namespace Ringly.Asterisk.Models.Orchestrations.Onboarding.Exceptions;

public class UserOnboardingValidationException : Xeption
{
    public UserOnboardingValidationException(Xeption innerException)
        : base("User onboarding validation error occurred, fix errors and try again.", innerException)
    { }
}
