using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnAddIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        SipEndpointConfig someConfig = CreateRandomSipEndpointConfig();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidSipEndpointConfigException = new InvalidSipEndpointConfigException();

        var expectedException =
            new SipEndpointConfigDependencyValidationException(invalidSipEndpointConfigException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask addTask = this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(someConfig);

        SipEndpointConfigDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigDependencyValidationException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnAddIfConflictErrorOccursAndLogItAsync()
    {
        // given
        SipEndpointConfig someConfig = CreateRandomSipEndpointConfig();
        var httpResponseConflictException = new HttpResponseConflictException();
        var duplicateExtensionException = new DuplicateExtensionException(httpResponseConflictException);

        var expectedException =
            new SipEndpointConfigDependencyValidationException(duplicateExtensionException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(httpResponseConflictException);

        // when
        ValueTask addTask = this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(someConfig);

        SipEndpointConfigDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigDependencyValidationException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnAddIfNotFoundErrorOccursAndLogItAsync()
    {
        // given
        SipEndpointConfig someConfig = CreateRandomSipEndpointConfig();
        var httpResponseNotFoundException = new HttpResponseNotFoundException();
        var notFoundSipEndpointConfigException = new NotFoundSipEndpointConfigException(httpResponseNotFoundException);

        var failedAsteriskSipEndpointConfigDependencyException =
            new FailedAsteriskSipEndpointConfigDependencyException(notFoundSipEndpointConfigException);

        var expectedException =
            new SipEndpointConfigDependencyException(failedAsteriskSipEndpointConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(httpResponseNotFoundException);

        // when
        ValueTask addTask = this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(someConfig);

        SipEndpointConfigDependencyException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigDependencyException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> CriticalDependencyExceptions() =>
    [
        new HttpResponseUnauthorizedException(),
        new HttpResponseForbiddenException(),
        new HttpRequestException()
    ];

    [Theory]
    [MemberData(nameof(CriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnAddIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        SipEndpointConfig someConfig = CreateRandomSipEndpointConfig();

        var failedAsteriskSipEndpointConfigDependencyException =
            new FailedAsteriskSipEndpointConfigDependencyException(dependencyException);

        var expectedException =
            new SipEndpointConfigDependencyException(failedAsteriskSipEndpointConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask addTask = this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(someConfig);

        SipEndpointConfigDependencyException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigDependencyException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> NonCriticalDependencyExceptions() =>
    [
        new HttpResponseInternalServerErrorException(),
        new HttpResponseServiceUnavailableException()
    ];

    [Theory]
    [MemberData(nameof(NonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnAddIfErrorOccursAndLogItAsync(Exception dependencyException)
    {
        // given
        SipEndpointConfig someConfig = CreateRandomSipEndpointConfig();

        var failedAsteriskSipEndpointConfigDependencyException =
            new FailedAsteriskSipEndpointConfigDependencyException(dependencyException);

        var expectedException =
            new SipEndpointConfigDependencyException(failedAsteriskSipEndpointConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask addTask = this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(someConfig);

        SipEndpointConfigDependencyException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigDependencyException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnAddIfErrorOccursAndLogItAsync()
    {
        // given
        SipEndpointConfig someConfig = CreateRandomSipEndpointConfig();
        var exception = new Exception();
        var failedSipEndpointConfigServiceException = new FailedSipEndpointConfigServiceException(exception);

        var expectedException =
            new SipEndpointConfigServiceException(failedSipEndpointConfigServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(exception);

        // when
        ValueTask addTask = this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(someConfig);

        SipEndpointConfigServiceException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigServiceException>(addTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
