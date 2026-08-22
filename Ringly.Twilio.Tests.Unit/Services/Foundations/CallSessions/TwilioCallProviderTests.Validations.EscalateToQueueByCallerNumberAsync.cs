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
        ValueTask<CallSession> escalateTask = this.twilioCallProvider
            .EscalateToQueueByCallerNumberAsync(invalidCallerNumber!, someQueueName);

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
        ValueTask<CallSession> escalateTask = this.twilioCallProvider
            .EscalateToQueueByCallerNumberAsync(someCallerNumber, invalidQueueName!);

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

    [Fact]
    public async Task ShouldThrowValidationExceptionOnEscalateToQueueByCallerNumberIfChannelNotFoundAndLogItAsync()
    {
        // given
        string someCallerNumber = GetRandomString();
        string someQueueName = GetRandomString();
        string? nullCallSid = null;

        var notFoundChannelException = new NotFoundChannelException(someCallerNumber);

        var expectedValidationException =
            new CallSessionValidationException(notFoundChannelException);

        this.twilioBrokerMock.Setup(broker =>
            broker.RetrieveCallSidByCallerNumberAsync(someCallerNumber))
                .ReturnsAsync(nullCallSid);

        // when
        ValueTask<CallSession> escalateTask = this.twilioCallProvider
            .EscalateToQueueByCallerNumberAsync(someCallerNumber, someQueueName);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.twilioBrokerMock.Verify(broker =>
            broker.RetrieveCallSidByCallerNumberAsync(someCallerNumber),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
