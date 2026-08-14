using System.Reactive.Linq;
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
            broker.InsertChannelAsync($"PJSIP/{partyA.SipExtension}"))
                .ReturnsAsync(returnedChannelA);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync($"PJSIP/{partyB.SipExtension}"))
                .ReturnsAsync(returnedChannelB);

        // A cold observable replaying both events to every independent subscription — the
        // service subscribes once per channel it's waiting on, in sequence, each filtering out
        // the other channel's event.
        this.asteriskBrokerMock.Setup(broker =>
            broker.StreamStasisStartEvents())
                .Returns(new[]
                {
                    new StasisStartEvent { ChannelId = returnedChannelA.ChannelId },
                    new StasisStartEvent { ChannelId = returnedChannelB.ChannelId }
                }.ToObservable());

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
            broker.InsertChannelAsync($"PJSIP/{partyA.SipExtension}"),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync($"PJSIP/{partyB.SipExtension}"),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.AddChannelToBridgeAsync(returnedBridge.Id, returnedChannelA.ChannelId),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.AddChannelToBridgeAsync(returnedBridge.Id, returnedChannelB.ChannelId),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.StreamStasisStartEvents(),
                Times.Exactly(2));

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
