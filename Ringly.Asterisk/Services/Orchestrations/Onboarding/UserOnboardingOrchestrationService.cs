using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;

namespace Ringly.Asterisk.Services.Orchestrations.Onboarding;

public partial class UserOnboardingOrchestrationService : IUserOnboardingOrchestrationService
{
    private readonly ICallProvisioningService callProvisioningService;
    private readonly ILoggingBroker loggingBroker;

    public UserOnboardingOrchestrationService(
        ICallProvisioningService callProvisioningService,
        ILoggingBroker loggingBroker)
    {
        this.callProvisioningService = callProvisioningService;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<SipCredentials> OnboardClientForCallingAsync(Guid clientId) =>
    TryCatch(async () =>
        await this.callProvisioningService.AddClientCredentialsAsync(clientId));
}
