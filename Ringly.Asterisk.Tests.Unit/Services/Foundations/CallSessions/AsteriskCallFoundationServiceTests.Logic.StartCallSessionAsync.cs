using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Fact]
    public async Task ShouldStartCallSessionAsync()
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();
        Bridge returnedBridge = CreateRandomBridge();
        Channel returnedChannelA = CreateRandomChannel();
        Channel returnedChannelB = CreateRandomChannel();

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("mixing"))
                .ReturnsAsync(returnedBridge);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync(partyA.SipExtension))
                .ReturnsAsync(returnedChannelA);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync(partyB.SipExtension))
                .ReturnsAsync(returnedChannelB);

        this.asteriskBrokerMock.Setup(broker =>
            broker.AddChannelToBridgeAsync(returnedBridge.Id, returnedChannelA.ChannelId))
                .Returns(ValueTask.CompletedTask);

        this.asteriskBrokerMock.Setup(broker =>
            broker.AddChannelToBridgeAsync(returnedBridge.Id, returnedChannelB.ChannelId))
                .Returns(ValueTask.CompletedTask);

        // when
        CallSession actualCallSession =
            await this.callFoundationService.StartCallSessionAsync(partyA, partyB);

        // then
        actualCallSession.BridgeId.Should().Be(returnedBridge.Id);
        actualCallSession.CallSessionId.Should().NotBeEmpty();

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("mixing"),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync(partyA.SipExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync(partyB.SipExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.AddChannelToBridgeAsync(returnedBridge.Id, returnedChannelA.ChannelId),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.AddChannelToBridgeAsync(returnedBridge.Id, returnedChannelB.ChannelId),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
