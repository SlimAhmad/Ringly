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
    public async Task ShouldThrowValidationExceptionOnEscalateToQueueIfChannelIdIsInvalidAndLogItAsync(
        string? invalidChannelId)
    {
        // given
        string someQueueName = GetRandomString();

        var invalidEscalateToQueueRequestException = new InvalidEscalateToQueueRequestException();

        invalidEscalateToQueueRequestException.UpsertDataList(
            key: "channelId",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidEscalateToQueueRequestException);

        // when
        ValueTask<CallSession> escalateTask =
            this.callFoundationService.EscalateToQueueAsync(invalidChannelId!, someQueueName);

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
    public async Task ShouldThrowValidationExceptionOnEscalateToQueueIfQueueNameIsInvalidAndLogItAsync(
        string? invalidQueueName)
    {
        // given
        string someChannelId = GetRandomString();

        var invalidEscalateToQueueRequestException = new InvalidEscalateToQueueRequestException();

        invalidEscalateToQueueRequestException.UpsertDataList(
            key: "queueName",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidEscalateToQueueRequestException);

        // when
        ValueTask<CallSession> escalateTask =
            this.callFoundationService.EscalateToQueueAsync(someChannelId, invalidQueueName!);

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
    public async Task ShouldThrowValidationExceptionOnEscalateToQueueIfQueueNotFoundAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        string someQueueName = GetRandomString();
        HoldingBridge? nullHoldingBridge = null;

        var notFoundQueueException = new NotFoundQueueException(someQueueName);

        var expectedValidationException =
            new CallSessionValidationException(notFoundQueueException);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(nullHoldingBridge);

        // when
        ValueTask<CallSession> escalateTask =
            this.callFoundationService.EscalateToQueueAsync(someChannelId, someQueueName);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

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
