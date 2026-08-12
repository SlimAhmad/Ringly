using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnSendTransferProgressIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        TransferState someState = TransferState.ChannelAnswered;
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidTransferProgressRequestException = new InvalidTransferProgressRequestException();

        var expectedException =
            new TransferDependencyValidationException(invalidTransferProgressRequestException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState))
                .ThrowsAsync(httpResponseBadRequestException);

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

    public static TheoryData<Exception> TransferCriticalDependencyExceptions() =>
    [
        new HttpResponseUnauthorizedException(),
        new HttpResponseForbiddenException(),
        new HttpResponseNotFoundException(),
        new HttpRequestException()
    ];

    [Theory]
    [MemberData(nameof(TransferCriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnSendTransferProgressIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someChannelId = GetRandomString();
        TransferState someState = TransferState.ChannelAnswered;

        var failedAsteriskTransferDependencyException =
            new FailedAsteriskTransferDependencyException(dependencyException);

        var expectedException =
            new TransferDependencyException(failedAsteriskTransferDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState))
                .ThrowsAsync(dependencyException);

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

    public static TheoryData<Exception> TransferNonCriticalDependencyExceptions() =>
    [
        new HttpResponseInternalServerErrorException(),
        new HttpResponseServiceUnavailableException()
    ];

    [Theory]
    [MemberData(nameof(TransferNonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnSendTransferProgressIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someChannelId = GetRandomString();
        TransferState someState = TransferState.ChannelAnswered;

        var failedAsteriskTransferDependencyException =
            new FailedAsteriskTransferDependencyException(dependencyException);

        var expectedException =
            new TransferDependencyException(failedAsteriskTransferDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SendTransferProgressAsync(someChannelId, someState))
                .ThrowsAsync(dependencyException);

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
