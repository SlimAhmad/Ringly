using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Tests.Unit.Services.Orchestrations.Onboarding;

public partial class UserOnboardingOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldOnboardClientForCallingAsync()
    {
        // given
        Guid inputClientId = GetRandomId();
        SipCredentials returnedCredentials = CreateRandomSipCredentials();

        this.callProvisioningServiceMock.Setup(service =>
            service.AddClientCredentialsAsync(inputClientId))
                .ReturnsAsync(returnedCredentials);

        // when
        SipCredentials actualCredentials =
            await this.userOnboardingOrchestrationService.OnboardClientForCallingAsync(inputClientId);

        // then
        actualCredentials.Should().BeEquivalentTo(returnedCredentials);

        this.callProvisioningServiceMock.Verify(service =>
            service.AddClientCredentialsAsync(inputClientId),
                Times.Once);

        this.callProvisioningServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
