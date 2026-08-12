using System.Net;
using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnAddIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        SipEndpointConfig someConfig = CreateRandomSipEndpointConfig();

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: HttpStatusCode.BadRequest);

        var invalidSipEndpointConfigException = new InvalidSipEndpointConfigException();

        var expectedException =
            new SipEndpointConfigDependencyValidationException(invalidSipEndpointConfigException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(httpRequestException);

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

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: HttpStatusCode.Conflict);

        var duplicateExtensionException = new DuplicateExtensionException(httpRequestException);

        var expectedException =
            new SipEndpointConfigDependencyValidationException(duplicateExtensionException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(httpRequestException);

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

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: HttpStatusCode.NotFound);

        var notFoundSipEndpointConfigException = new NotFoundSipEndpointConfigException(httpRequestException);

        var failedAsteriskSipEndpointConfigDependencyException =
            new FailedAsteriskSipEndpointConfigDependencyException(notFoundSipEndpointConfigException);

        var expectedException =
            new SipEndpointConfigDependencyException(failedAsteriskSipEndpointConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(httpRequestException);

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

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ShouldThrowCriticalDependencyExceptionOnAddIfErrorOccursAndLogItAsync(
        HttpStatusCode httpStatusCode)
    {
        // given
        SipEndpointConfig someConfig = CreateRandomSipEndpointConfig();

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: httpStatusCode);

        var failedAsteriskSipEndpointConfigDependencyException =
            new FailedAsteriskSipEndpointConfigDependencyException(httpRequestException);

        var expectedException =
            new SipEndpointConfigDependencyException(failedAsteriskSipEndpointConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(httpRequestException);

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

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnAddIfHttpRequestErrorOccursAndLogItAsync()
    {
        // given
        SipEndpointConfig someConfig = CreateRandomSipEndpointConfig();
        var httpRequestException = new HttpRequestException(GetRandomString());

        var failedAsteriskSipEndpointConfigDependencyException =
            new FailedAsteriskSipEndpointConfigDependencyException(httpRequestException);

        var expectedException =
            new SipEndpointConfigDependencyException(failedAsteriskSipEndpointConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(httpRequestException);

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

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ShouldThrowDependencyExceptionOnAddIfErrorOccursAndLogItAsync(HttpStatusCode httpStatusCode)
    {
        // given
        SipEndpointConfig someConfig = CreateRandomSipEndpointConfig();

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: httpStatusCode);

        var failedAsteriskSipEndpointConfigDependencyException =
            new FailedAsteriskSipEndpointConfigDependencyException(httpRequestException);

        var expectedException =
            new SipEndpointConfigDependencyException(failedAsteriskSipEndpointConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(someConfig))
                .ThrowsAsync(httpRequestException);

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
