using FluentAssertions;
using Moq;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnRemoveIfExtensionNotFoundAndLogItAsync()
    {
        // given
        string inputExtension = GetRandomString();

        var extensionNotFoundException = new ExtensionNotFoundException(
            new InvalidOperationException($"Extension '{inputExtension}' is not provisioned."));

        var expectedException =
            new SipEndpointConfigDependencyValidationException(extensionNotFoundException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension))
                .ThrowsAsync(new HttpResponseNotFoundException());

        // when
        ValueTask removeTask =
            this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(inputExtension);

        SipEndpointConfigDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigDependencyValidationException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnRemoveIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string inputExtension = GetRandomString();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidSipEndpointConfigException = new InvalidSipEndpointConfigException();

        var expectedException =
            new SipEndpointConfigDependencyValidationException(invalidSipEndpointConfigException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension))
                .ReturnsAsync([]);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask removeTask =
            this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(inputExtension);

        SipEndpointConfigDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigDependencyValidationException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> RemoveCriticalDependencyExceptions() =>
    [
        new HttpResponseUnauthorizedException(),
        new HttpResponseForbiddenException(),
        new HttpRequestException()
    ];

    [Theory]
    [MemberData(nameof(RemoveCriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnRemoveIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string inputExtension = GetRandomString();

        var failedAsteriskSipEndpointConfigDependencyException =
            new FailedAsteriskSipEndpointConfigDependencyException(dependencyException);

        var expectedException =
            new SipEndpointConfigDependencyException(failedAsteriskSipEndpointConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension))
                .ReturnsAsync([]);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask removeTask =
            this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(inputExtension);

        SipEndpointConfigDependencyException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigDependencyException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> RemoveNonCriticalDependencyExceptions() =>
    [
        new HttpResponseInternalServerErrorException(),
        new HttpResponseServiceUnavailableException()
    ];

    [Theory]
    [MemberData(nameof(RemoveNonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnRemoveIfErrorOccursAndLogItAsync(Exception dependencyException)
    {
        // given
        string inputExtension = GetRandomString();

        var failedAsteriskSipEndpointConfigDependencyException =
            new FailedAsteriskSipEndpointConfigDependencyException(dependencyException);

        var expectedException =
            new SipEndpointConfigDependencyException(failedAsteriskSipEndpointConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension))
                .ReturnsAsync([]);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask removeTask =
            this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(inputExtension);

        SipEndpointConfigDependencyException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigDependencyException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRemoveIfErrorOccursAndLogItAsync()
    {
        // given
        string inputExtension = GetRandomString();
        var exception = new Exception();
        var failedSipEndpointConfigServiceException = new FailedSipEndpointConfigServiceException(exception);

        var expectedException =
            new SipEndpointConfigServiceException(failedSipEndpointConfigServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension))
                .ReturnsAsync([]);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension))
                .ThrowsAsync(exception);

        // when
        ValueTask removeTask =
            this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(inputExtension);

        SipEndpointConfigServiceException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigServiceException>(removeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
