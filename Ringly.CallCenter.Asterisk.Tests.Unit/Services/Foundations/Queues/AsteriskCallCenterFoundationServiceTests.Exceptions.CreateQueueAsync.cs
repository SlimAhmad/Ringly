using System.Net;
using FluentAssertions;
using Moq;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnCreateQueueIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(),
            inner: null,
            statusCode: HttpStatusCode.BadRequest);

        var invalidQueueConfigException = new InvalidQueueConfigException();

        var expectedQueueConfigDependencyValidationException =
            new QueueConfigDependencyValidationException(invalidQueueConfigException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("holding"))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.asteriskCallCenterFoundationService.CreateQueueAsync(someQueueConfig);

        QueueConfigDependencyValidationException actualException =
            await Assert.ThrowsAsync<QueueConfigDependencyValidationException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigDependencyValidationException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("holding"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedQueueConfigDependencyValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnCreateQueueIfConflictErrorOccursAndLogItAsync()
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(),
            inner: null,
            statusCode: HttpStatusCode.Conflict);

        var alreadyExistsQueueConfigException = new AlreadyExistsQueueConfigException(httpRequestException);

        var expectedQueueConfigDependencyValidationException =
            new QueueConfigDependencyValidationException(alreadyExistsQueueConfigException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("holding"))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.asteriskCallCenterFoundationService.CreateQueueAsync(someQueueConfig);

        QueueConfigDependencyValidationException actualException =
            await Assert.ThrowsAsync<QueueConfigDependencyValidationException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigDependencyValidationException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("holding"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedQueueConfigDependencyValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ShouldThrowCriticalDependencyExceptionOnCreateQueueIfErrorOccursAndLogItAsync(
        HttpStatusCode httpStatusCode)
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(),
            inner: null,
            statusCode: httpStatusCode);

        var failedAsteriskQueueConfigDependencyException =
            new FailedAsteriskQueueConfigDependencyException(httpRequestException);

        var expectedQueueConfigDependencyException =
            new QueueConfigDependencyException(failedAsteriskQueueConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("holding"))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.asteriskCallCenterFoundationService.CreateQueueAsync(someQueueConfig);

        QueueConfigDependencyException actualException =
            await Assert.ThrowsAsync<QueueConfigDependencyException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigDependencyException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("holding"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedQueueConfigDependencyException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnCreateQueueIfHttpRequestErrorOccursAndLogItAsync()
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();
        var httpRequestException = new HttpRequestException(GetRandomString());

        var failedAsteriskQueueConfigDependencyException =
            new FailedAsteriskQueueConfigDependencyException(httpRequestException);

        var expectedQueueConfigDependencyException =
            new QueueConfigDependencyException(failedAsteriskQueueConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("holding"))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.asteriskCallCenterFoundationService.CreateQueueAsync(someQueueConfig);

        QueueConfigDependencyException actualException =
            await Assert.ThrowsAsync<QueueConfigDependencyException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigDependencyException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("holding"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedQueueConfigDependencyException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ShouldThrowDependencyExceptionOnCreateQueueIfErrorOccursAndLogItAsync(
        HttpStatusCode httpStatusCode)
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(),
            inner: null,
            statusCode: httpStatusCode);

        var failedAsteriskQueueConfigDependencyException =
            new FailedAsteriskQueueConfigDependencyException(httpRequestException);

        var expectedQueueConfigDependencyException =
            new QueueConfigDependencyException(failedAsteriskQueueConfigDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("holding"))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.asteriskCallCenterFoundationService.CreateQueueAsync(someQueueConfig);

        QueueConfigDependencyException actualException =
            await Assert.ThrowsAsync<QueueConfigDependencyException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigDependencyException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("holding"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedQueueConfigDependencyException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnCreateQueueIfErrorOccursAndLogItAsync()
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();
        var exception = new Exception();

        var failedQueueConfigServiceException =
            new FailedQueueConfigServiceException(exception);

        var expectedQueueConfigServiceException =
            new QueueConfigServiceException(failedQueueConfigServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("holding"))
                .ThrowsAsync(exception);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.asteriskCallCenterFoundationService.CreateQueueAsync(someQueueConfig);

        QueueConfigServiceException actualException =
            await Assert.ThrowsAsync<QueueConfigServiceException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigServiceException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("holding"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedQueueConfigServiceException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
