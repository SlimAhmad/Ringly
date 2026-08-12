using Moq;
using Ringly.Abstractions.Models;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldSendTransferProgressAsync()
    {
        // given
        string inputChannelId = GetRandomString();
        TransferState inputState = TransferState.ChannelAnswered;

        this.asteriskBrokerMock.Setup(broker =>
            broker.SendTransferProgressAsync(inputChannelId, inputState))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.asteriskCallCenterFoundationService.SendTransferProgressAsync(inputChannelId, inputState);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.SendTransferProgressAsync(inputChannelId, inputState),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
