using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Twilio.Models.Foundations.Transfers.Exceptions;

namespace Ringly.CallCenter.Twilio.Tests.Unit.Services.Foundations.Queues;

public partial class TwilioCallCenterProviderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnSendTransferProgressIfChannelIdIsInvalidAndLogItAsync(
        string? invalidChannelId)
    {
        // given
        TransferState someState = TransferState.ChannelAnswered;

        var invalidTransferProgressRequestException = new InvalidTransferProgressRequestException();

        invalidTransferProgressRequestException.UpsertDataList(
            key: "channelId",
            value: "Value is required");

        var expectedValidationException =
            new TransferValidationException(invalidTransferProgressRequestException);

        // when
        ValueTask sendTask = this.twilioCallCenterProvider.SendTransferProgressAsync(
            invalidChannelId!, someState);

        TransferValidationException actualException =
            await Assert.ThrowsAsync<TransferValidationException>(sendTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
