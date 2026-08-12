using System.Net;
using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnStartIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: HttpStatusCode.BadRequest);

        var invalidCallParticipantException = new InvalidCallParticipantException();

        var expectedException =
            new CallSessionDependencyValidationException(invalidCallParticipantException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("mixing"))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<CallSession> startTask =
            this.callFoundationService.StartCallSessionAsync(partyA, partyB);

        CallSessionDependencyValidationException actualException =
            await Assert.ThrowsAsync<CallSessionDependencyValidationException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("mixing"),
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
    public async Task ShouldThrowCriticalDependencyExceptionOnStartIfErrorOccursAndLogItAsync(
        HttpStatusCode httpStatusCode)
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: httpStatusCode);

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(httpRequestException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("mixing"))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<CallSession> startTask =
            this.callFoundationService.StartCallSessionAsync(partyA, partyB);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("mixing"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnStartIfHttpRequestErrorOccursAndLogItAsync()
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();
        var httpRequestException = new HttpRequestException(GetRandomString());

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(httpRequestException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("mixing"))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<CallSession> startTask =
            this.callFoundationService.StartCallSessionAsync(partyA, partyB);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("mixing"),
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
    public async Task ShouldThrowDependencyExceptionOnStartIfErrorOccursAndLogItAsync(HttpStatusCode httpStatusCode)
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();

        var httpRequestException = new HttpRequestException(
            message: GetRandomString(), inner: null, statusCode: httpStatusCode);

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(httpRequestException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("mixing"))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<CallSession> startTask =
            this.callFoundationService.StartCallSessionAsync(partyA, partyB);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("mixing"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnStartIfErrorOccursAndLogItAsync()
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();
        var exception = new Exception();
        var failedCallProviderServiceException = new FailedCallProviderServiceException(exception);

        var expectedException =
            new CallProviderServiceException(failedCallProviderServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("mixing"))
                .ThrowsAsync(exception);

        // when
        ValueTask<CallSession> startTask =
            this.callFoundationService.StartCallSessionAsync(partyA, partyB);

        CallProviderServiceException actualException =
            await Assert.ThrowsAsync<CallProviderServiceException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("mixing"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
