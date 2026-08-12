using System.Net;
using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnSendTransferProgressIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        TransferState someState = TransferState.ChannelAnswered;

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: HttpStatusCode.BadRequest);

        var invalidTransferProgressRequestException = new InvalidTransferProgressRequestException();

        var expectedException =
            new TransferDependencyValidationException(invalidTransferProgressRequestException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask sendTask =
            this.asteriskCallCenterFoundationService.SendTransferProgressAsync(someChannelId, someState);

        TransferDependencyValidationException actualException =
            await Assert.ThrowsAsync<TransferDependencyValidationException>(sendTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ShouldThrowCriticalDependencyExceptionOnSendTransferProgressIfErrorOccursAndLogItAsync(
        HttpStatusCode httpStatusCode)
    {
        // given
        string someChannelId = GetRandomString();
        TransferState someState = TransferState.ChannelAnswered;

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: httpStatusCode);

        var failedAsteriskTransferDependencyException =
            new FailedAsteriskTransferDependencyException(httpRequestException);

        var expectedException =
            new TransferDependencyException(failedAsteriskTransferDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask sendTask =
            this.asteriskCallCenterFoundationService.SendTransferProgressAsync(someChannelId, someState);

        TransferDependencyException actualException =
            await Assert.ThrowsAsync<TransferDependencyException>(sendTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnSendTransferProgressIfHttpRequestErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        TransferState someState = TransferState.ChannelAnswered;
        var httpRequestException = new HttpRequestException(GetRandomString());

        var failedAsteriskTransferDependencyException =
            new FailedAsteriskTransferDependencyException(httpRequestException);

        var expectedException =
            new TransferDependencyException(failedAsteriskTransferDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask sendTask =
            this.asteriskCallCenterFoundationService.SendTransferProgressAsync(someChannelId, someState);

        TransferDependencyException actualException =
            await Assert.ThrowsAsync<TransferDependencyException>(sendTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ShouldThrowDependencyExceptionOnSendTransferProgressIfErrorOccursAndLogItAsync(
        HttpStatusCode httpStatusCode)
    {
        // given
        string someChannelId = GetRandomString();
        TransferState someState = TransferState.ChannelAnswered;

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: httpStatusCode);

        var failedAsteriskTransferDependencyException =
            new FailedAsteriskTransferDependencyException(httpRequestException);

        var expectedException =
            new TransferDependencyException(failedAsteriskTransferDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask sendTask =
            this.asteriskCallCenterFoundationService.SendTransferProgressAsync(someChannelId, someState);

        TransferDependencyException actualException =
            await Assert.ThrowsAsync<TransferDependencyException>(sendTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnSendTransferProgressIfErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        TransferState someState = TransferState.ChannelAnswered;
        var exception = new Exception();
        var failedTransferServiceException = new FailedTransferServiceException(exception);

        var expectedException =
            new TransferServiceException(failedTransferServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState))
                .ThrowsAsync(exception);

        // when
        ValueTask sendTask =
            this.asteriskCallCenterFoundationService.SendTransferProgressAsync(someChannelId, someState);

        TransferServiceException actualException =
            await Assert.ThrowsAsync<TransferServiceException>(sendTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
