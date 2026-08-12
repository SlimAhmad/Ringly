using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnStartIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidCallParticipantException = new InvalidCallParticipantException();

        var expectedException =
            new CallSessionDependencyValidationException(invalidCallParticipantException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("mixing"))
                .ThrowsAsync(httpResponseBadRequestException);

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

    public static TheoryData<Exception> CriticalDependencyExceptions() =>
    [
        new HttpResponseUnauthorizedException(),
        new HttpResponseForbiddenException(),
        new HttpResponseNotFoundException(),
        new HttpRequestException()
    ];

    [Theory]
    [MemberData(nameof(CriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnStartIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(dependencyException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("mixing"))
                .ThrowsAsync(dependencyException);

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

    public static TheoryData<Exception> NonCriticalDependencyExceptions() =>
    [
        new HttpResponseInternalServerErrorException(),
        new HttpResponseServiceUnavailableException()
    ];

    [Theory]
    [MemberData(nameof(NonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnStartIfErrorOccursAndLogItAsync(Exception dependencyException)
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(dependencyException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("mixing"))
                .ThrowsAsync(dependencyException);

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
