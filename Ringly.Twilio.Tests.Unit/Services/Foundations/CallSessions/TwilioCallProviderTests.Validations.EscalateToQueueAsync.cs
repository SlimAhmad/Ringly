using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
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
            this.twilioCallProvider.EscalateToQueueAsync(invalidChannelId!, someQueueName);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
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
            this.twilioCallProvider.EscalateToQueueAsync(someChannelId, invalidQueueName!);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
