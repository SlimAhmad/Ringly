using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnEscalateToQueueByCallerNumberIfCallerNumberIsInvalidAndLogItAsync(
        string? invalidCallerNumber)
    {
        // given
        string someQueueName = GetRandomString();

        var invalidEscalateToQueueRequestException = new InvalidEscalateToQueueRequestException();

        invalidEscalateToQueueRequestException.UpsertDataList(
            key: "callerNumber",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidEscalateToQueueRequestException);

        // when
        ValueTask<CallSession> escalateTask = this.callFoundationService
            .EscalateToQueueByCallerNumberAsync(invalidCallerNumber!, someQueueName);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnEscalateToQueueByCallerNumberIfQueueNameIsInvalidAndLogItAsync(
        string? invalidQueueName)
    {
        // given
        string someCallerNumber = GetRandomString();

        var invalidEscalateToQueueRequestException = new InvalidEscalateToQueueRequestException();

        invalidEscalateToQueueRequestException.UpsertDataList(
            key: "queueName",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidEscalateToQueueRequestException);

        // when
        ValueTask<CallSession> escalateTask = this.callFoundationService
            .EscalateToQueueByCallerNumberAsync(someCallerNumber, invalidQueueName!);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnEscalateToQueueByCallerNumberIfChannelNotFoundAndLogItAsync()
    {
        // given
        string someCallerNumber = GetRandomString();
        string someQueueName = GetRandomString();
        string? nullChannelId = null;

        var notFoundChannelException = new NotFoundChannelException(someCallerNumber);

        var expectedValidationException =
            new CallSessionValidationException(notFoundChannelException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RetrieveChannelIdByCallerNumberAsync(someCallerNumber))
                .ReturnsAsync(nullChannelId);

        // when
        ValueTask<CallSession> escalateTask = this.callFoundationService
            .EscalateToQueueByCallerNumberAsync(someCallerNumber, someQueueName);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RetrieveChannelIdByCallerNumberAsync(someCallerNumber),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnEscalateToQueueByCallerNumberIfQueueNotFoundAndLogItAsync()
    {
        // given
        string someCallerNumber = GetRandomString();
        string someChannelId = GetRandomString();
        string someQueueName = GetRandomString();
        HoldingBridge? nullHoldingBridge = null;

        var notFoundQueueException = new NotFoundQueueException(someQueueName);

        var expectedValidationException =
            new CallSessionValidationException(notFoundQueueException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RetrieveChannelIdByCallerNumberAsync(someCallerNumber))
                .ReturnsAsync(someChannelId);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(nullHoldingBridge);

        // when
        ValueTask<CallSession> escalateTask = this.callFoundationService
            .EscalateToQueueByCallerNumberAsync(someCallerNumber, someQueueName);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RetrieveChannelIdByCallerNumberAsync(someCallerNumber),
                Times.Once);

        this.queueRegistryMock.Verify(registry =>
            registry.RetrieveByNameAsync(someQueueName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
