using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
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
        ValueTask sendTask = this.asteriskCallCenterFoundationService.SendTransferProgressAsync(
            invalidChannelId!, someState);

        TransferValidationException actualException =
            await Assert.ThrowsAsync<TransferValidationException>(sendTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
