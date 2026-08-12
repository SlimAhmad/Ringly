using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Services.Orchestrations.Onboarding;

public interface IUserOnboardingOrchestrationService
{
    ValueTask<SipCredentials> OnboardClientForCallingAsync(Guid clientId);
}
