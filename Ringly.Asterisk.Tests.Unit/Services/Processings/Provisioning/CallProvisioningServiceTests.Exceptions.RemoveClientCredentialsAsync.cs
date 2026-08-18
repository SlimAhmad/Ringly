using FluentAssertions;
using Moq;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;
using Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Processings.Provisioning;

public partial class CallProvisioningServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnRemoveIfFoundationValidationErrorOccursAndLogItAsync()
    {
        // given
        string inputExtension = GetRandomExtension();

        var sipEndpointConfigValidationException =
            new SipEndpointConfigValidationException(new InvalidSipEndpointConfigException());

        var expectedValidationException =
            new SipCredentialsValidationException(sipEndpointConfigValidationException);

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension))
                .ThrowsAsync(sipEndpointConfigValidationException);

        // when
        ValueTask removeTask =
            this.callProvisioningService.RemoveClientCredentialsAsync(inputExtension);

        SipCredentialsValidationException actualException =
            await Assert.ThrowsAsync<SipCredentialsValidationException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnRemoveIfFoundationDependencyValidationErrorOccursAndLogItAsync()
    {
        // given
        string inputExtension = GetRandomExtension();

        var sipEndpointConfigDependencyValidationException =
            new SipEndpointConfigDependencyValidationException(new InvalidSipEndpointConfigException());

        var expectedException =
            new SipCredentialsDependencyValidationException(sipEndpointConfigDependencyValidationException);

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension))
                .ThrowsAsync(sipEndpointConfigDependencyValidationException);

        // when
        ValueTask removeTask =
            this.callProvisioningService.RemoveClientCredentialsAsync(inputExtension);

        SipCredentialsDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipCredentialsDependencyValidationException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyExceptionOnRemoveIfFoundationDependencyErrorOccursAndLogItAsync()
    {
        // given
        string inputExtension = GetRandomExtension();

        var sipEndpointConfigDependencyException =
            new SipEndpointConfigDependencyException(
                new FailedAsteriskSipEndpointConfigDependencyException(new Exception()));

        var expectedException =
            new SipCredentialsDependencyException(sipEndpointConfigDependencyException);

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension))
                .ThrowsAsync(sipEndpointConfigDependencyException);

        // when
        ValueTask removeTask =
            this.callProvisioningService.RemoveClientCredentialsAsync(inputExtension);

        SipCredentialsDependencyException actualException =
            await Assert.ThrowsAsync<SipCredentialsDependencyException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRemoveIfFoundationServiceErrorOccursAndLogItAsync()
    {
        // given
        string inputExtension = GetRandomExtension();

        var sipEndpointConfigServiceException =
            new SipEndpointConfigServiceException(new FailedSipEndpointConfigServiceException(new Exception()));

        var expectedException =
            new SipCredentialsServiceException(sipEndpointConfigServiceException);

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension))
                .ThrowsAsync(sipEndpointConfigServiceException);

        // when
        ValueTask removeTask =
            this.callProvisioningService.RemoveClientCredentialsAsync(inputExtension);

        SipCredentialsServiceException actualException =
            await Assert.ThrowsAsync<SipCredentialsServiceException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRemoveIfErrorOccursAndLogItAsync()
    {
        // given
        string inputExtension = GetRandomExtension();
        var exception = new Exception();
        var failedSipCredentialsServiceException = new FailedSipCredentialsServiceException(exception);

        var expectedException =
            new SipCredentialsServiceException(failedSipCredentialsServiceException);

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension))
                .ThrowsAsync(exception);

        // when
        ValueTask removeTask =
            this.callProvisioningService.RemoveClientCredentialsAsync(inputExtension);

        SipCredentialsServiceException actualException =
            await Assert.ThrowsAsync<SipCredentialsServiceException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
