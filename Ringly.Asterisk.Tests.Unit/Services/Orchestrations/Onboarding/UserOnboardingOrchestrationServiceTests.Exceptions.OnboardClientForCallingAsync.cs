using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Orchestrations.Onboarding.Exceptions;
using Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Orchestrations.Onboarding;

public partial class UserOnboardingOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnOnboardIfProcessingValidationErrorOccursAndLogItAsync()
    {
        // given
        Guid inputClientId = GetRandomId();

        var sipCredentialsValidationException =
            new SipCredentialsValidationException(new InvalidSipCredentialsException());

        var expectedException =
            new UserOnboardingValidationException(sipCredentialsValidationException);

        this.callProvisioningServiceMock.Setup(service =>
            service.AddClientCredentialsAsync(inputClientId))
                .ThrowsAsync(sipCredentialsValidationException);

        // when
        ValueTask<SipCredentials> onboardTask =
            this.userOnboardingOrchestrationService.OnboardClientForCallingAsync(inputClientId);

        UserOnboardingValidationException actualException =
            await Assert.ThrowsAsync<UserOnboardingValidationException>(onboardTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.callProvisioningServiceMock.Verify(service =>
            service.AddClientCredentialsAsync(inputClientId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.callProvisioningServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnOnboardIfProcessingDependencyValidationErrorOccursAndLogItAsync()
    {
        // given
        Guid inputClientId = GetRandomId();

        var sipCredentialsDependencyValidationException =
            new SipCredentialsDependencyValidationException(new InvalidSipCredentialsException());

        var expectedException =
            new UserOnboardingDependencyValidationException(sipCredentialsDependencyValidationException);

        this.callProvisioningServiceMock.Setup(service =>
            service.AddClientCredentialsAsync(inputClientId))
                .ThrowsAsync(sipCredentialsDependencyValidationException);

        // when
        ValueTask<SipCredentials> onboardTask =
            this.userOnboardingOrchestrationService.OnboardClientForCallingAsync(inputClientId);

        UserOnboardingDependencyValidationException actualException =
            await Assert.ThrowsAsync<UserOnboardingDependencyValidationException>(onboardTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.callProvisioningServiceMock.Verify(service =>
            service.AddClientCredentialsAsync(inputClientId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.callProvisioningServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyExceptionOnOnboardIfProcessingDependencyErrorOccursAndLogItAsync()
    {
        // given
        Guid inputClientId = GetRandomId();

        var sipCredentialsDependencyException =
            new SipCredentialsDependencyException(
                new FailedSipCredentialsServiceException(new Exception()));

        var expectedException =
            new UserOnboardingDependencyException(sipCredentialsDependencyException);

        this.callProvisioningServiceMock.Setup(service =>
            service.AddClientCredentialsAsync(inputClientId))
                .ThrowsAsync(sipCredentialsDependencyException);

        // when
        ValueTask<SipCredentials> onboardTask =
            this.userOnboardingOrchestrationService.OnboardClientForCallingAsync(inputClientId);

        UserOnboardingDependencyException actualException =
            await Assert.ThrowsAsync<UserOnboardingDependencyException>(onboardTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.callProvisioningServiceMock.Verify(service =>
            service.AddClientCredentialsAsync(inputClientId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.callProvisioningServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnOnboardIfProcessingServiceErrorOccursAndLogItAsync()
    {
        // given
        Guid inputClientId = GetRandomId();

        var sipCredentialsServiceException =
            new SipCredentialsServiceException(new FailedSipCredentialsServiceException(new Exception()));

        var expectedException =
            new UserOnboardingServiceException(sipCredentialsServiceException);

        this.callProvisioningServiceMock.Setup(service =>
            service.AddClientCredentialsAsync(inputClientId))
                .ThrowsAsync(sipCredentialsServiceException);

        // when
        ValueTask<SipCredentials> onboardTask =
            this.userOnboardingOrchestrationService.OnboardClientForCallingAsync(inputClientId);

        UserOnboardingServiceException actualException =
            await Assert.ThrowsAsync<UserOnboardingServiceException>(onboardTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.callProvisioningServiceMock.Verify(service =>
            service.AddClientCredentialsAsync(inputClientId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.callProvisioningServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnOnboardIfErrorOccursAndLogItAsync()
    {
        // given
        Guid inputClientId = GetRandomId();
        var exception = new Exception();
        var failedUserOnboardingServiceException = new FailedUserOnboardingServiceException(exception);

        var expectedException =
            new UserOnboardingServiceException(failedUserOnboardingServiceException);

        this.callProvisioningServiceMock.Setup(service =>
            service.AddClientCredentialsAsync(inputClientId))
                .ThrowsAsync(exception);

        // when
        ValueTask<SipCredentials> onboardTask =
            this.userOnboardingOrchestrationService.OnboardClientForCallingAsync(inputClientId);

        UserOnboardingServiceException actualException =
            await Assert.ThrowsAsync<UserOnboardingServiceException>(onboardTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.callProvisioningServiceMock.Verify(service =>
            service.AddClientCredentialsAsync(inputClientId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.callProvisioningServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
