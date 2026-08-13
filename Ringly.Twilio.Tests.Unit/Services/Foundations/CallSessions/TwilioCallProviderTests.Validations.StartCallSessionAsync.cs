using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnStartIfPartyAIsNullAndLogItAsync()
    {
        // given
        CallParticipant nullPartyA = null!;
        CallParticipant partyB = CreateRandomCallParticipant();
        var nullCallParticipantException = new NullCallParticipantException();

        var expectedValidationException =
            new CallSessionValidationException(nullCallParticipantException);

        // when
        ValueTask<CallSession> startTask =
            this.twilioCallProvider.StartCallSessionAsync(nullPartyA, partyB);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnStartIfPartyBIsNullAndLogItAsync()
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant nullPartyB = null!;
        var nullCallParticipantException = new NullCallParticipantException();

        var expectedValidationException =
            new CallSessionValidationException(nullCallParticipantException);

        // when
        ValueTask<CallSession> startTask =
            this.twilioCallProvider.StartCallSessionAsync(partyA, nullPartyB);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnStartIfPartyAExtensionIsInvalidAndLogItAsync(
        string? invalidExtension)
    {
        // given
        CallParticipant invalidPartyA = CreateRandomCallParticipant();
        invalidPartyA.SipExtension = invalidExtension!;
        CallParticipant partyB = CreateRandomCallParticipant();

        var invalidCallParticipantException = new InvalidCallParticipantException();

        invalidCallParticipantException.UpsertDataList(
            key: nameof(CallParticipant.SipExtension),
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidCallParticipantException);

        // when
        ValueTask<CallSession> startTask =
            this.twilioCallProvider.StartCallSessionAsync(invalidPartyA, partyB);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
