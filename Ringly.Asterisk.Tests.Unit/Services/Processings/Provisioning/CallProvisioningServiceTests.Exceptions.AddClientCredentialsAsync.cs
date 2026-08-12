using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;
using Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Processings.Provisioning;

public partial class CallProvisioningServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnAddIfFoundationValidationErrorOccursAndLogItAsync()
    {
        // given
        Guid inputClientId = GetRandomId();

        var sipEndpointConfigValidationException =
            new SipEndpointConfigValidationException(new InvalidSipEndpointConfigException());

        var expectedValidationException =
            new SipCredentialsValidationException(sipEndpointConfigValidationException);

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()))
                .ThrowsAsync(sipEndpointConfigValidationException);

        // when
        ValueTask<SipCredentials> addTask =
            this.callProvisioningService.AddClientCredentialsAsync(inputClientId);

        SipCredentialsValidationException actualException =
            await Assert.ThrowsAsync<SipCredentialsValidationException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnAddIfFoundationDependencyValidationErrorOccursAndLogItAsync()
    {
        // given
        Guid inputClientId = GetRandomId();

        var sipEndpointConfigDependencyValidationException =
            new SipEndpointConfigDependencyValidationException(new InvalidSipEndpointConfigException());

        var expectedException =
            new SipCredentialsDependencyValidationException(sipEndpointConfigDependencyValidationException);

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()))
                .ThrowsAsync(sipEndpointConfigDependencyValidationException);

        // when
        ValueTask<SipCredentials> addTask =
            this.callProvisioningService.AddClientCredentialsAsync(inputClientId);

        SipCredentialsDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipCredentialsDependencyValidationException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyExceptionOnAddIfFoundationDependencyErrorOccursAndLogItAsync()
    {
        // given
        Guid inputClientId = GetRandomId();

        var sipEndpointConfigDependencyException =
            new SipEndpointConfigDependencyException(
                new FailedAsteriskSipEndpointConfigDependencyException(new Exception()));

        var expectedException =
            new SipCredentialsDependencyException(sipEndpointConfigDependencyException);

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()))
                .ThrowsAsync(sipEndpointConfigDependencyException);

        // when
        ValueTask<SipCredentials> addTask =
            this.callProvisioningService.AddClientCredentialsAsync(inputClientId);

        SipCredentialsDependencyException actualException =
            await Assert.ThrowsAsync<SipCredentialsDependencyException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnAddIfFoundationServiceErrorOccursAndLogItAsync()
    {
        // given
        Guid inputClientId = GetRandomId();

        var sipEndpointConfigServiceException =
            new SipEndpointConfigServiceException(new FailedSipEndpointConfigServiceException(new Exception()));

        var expectedException =
            new SipCredentialsServiceException(sipEndpointConfigServiceException);

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()))
                .ThrowsAsync(sipEndpointConfigServiceException);

        // when
        ValueTask<SipCredentials> addTask =
            this.callProvisioningService.AddClientCredentialsAsync(inputClientId);

        SipCredentialsServiceException actualException =
            await Assert.ThrowsAsync<SipCredentialsServiceException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnAddIfErrorOccursAndLogItAsync()
    {
        // given
        Guid inputClientId = GetRandomId();
        var exception = new Exception();
        var failedSipCredentialsServiceException = new FailedSipCredentialsServiceException(exception);

        var expectedException =
            new SipCredentialsServiceException(failedSipCredentialsServiceException);

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()))
                .ThrowsAsync(exception);

        // when
        ValueTask<SipCredentials> addTask =
            this.callProvisioningService.AddClientCredentialsAsync(inputClientId);

        SipCredentialsServiceException actualException =
            await Assert.ThrowsAsync<SipCredentialsServiceException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
