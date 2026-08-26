using System.Reactive.Linq;
using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Fact]
    public async Task ShouldConnectAgentToBridgeAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someAgentExtension = GetRandomString();
        Channel someAgentChannel = CreateRandomChannel();

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"))
                .ReturnsAsync(someAgentChannel);

        this.asteriskBrokerMock.Setup(broker =>
            broker.StreamStasisStartEvents())
                .Returns(new[] { new StasisStartEvent { ChannelId = someAgentChannel.ChannelId } }.ToObservable());

        var expectedAgentConnection = new AgentConnection
        {
            AgentChannelId = someAgentChannel.ChannelId,
            BridgeId = someBridgeId
        };

        // when
        AgentConnection actualAgentConnection =
            await this.callFoundationService.ConnectAgentToBridgeAsync(someBridgeId, someAgentExtension);

        // then
        actualAgentConnection.Should().BeEquivalentTo(expectedAgentConnection);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.StreamStasisStartEvents(),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.StopMusicOnHoldAsync(someBridgeId),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.AddChannelToBridgeAsync(someBridgeId, someAgentChannel.ChannelId),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
