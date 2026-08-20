using System.Reactive.Linq;
using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Fact]
    public async Task ShouldConnectAgentToQueueAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someCustomerChannelId = GetRandomString();
        string someAgentExtension = GetRandomString();
        Channel someAgentChannel = CreateRandomChannel();
        Bridge someTalkBridge = CreateRandomBridge();

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"))
                .ReturnsAsync(someAgentChannel);

        this.asteriskBrokerMock.Setup(broker =>
            broker.StreamStasisStartEvents())
                .Returns(new[] { new StasisStartEvent { ChannelId = someAgentChannel.ChannelId } }.ToObservable());

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("mixing"))
                .ReturnsAsync(someTalkBridge);

        // when
        Channel actualChannel = await this.callFoundationService.ConnectAgentToQueueAsync(
            someBridgeId, someCustomerChannelId, someAgentExtension);

        // then
        actualChannel.Should().BeEquivalentTo(someAgentChannel);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.StreamStasisStartEvents(),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("mixing"),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveChannelFromBridgeAsync(someBridgeId, someCustomerChannelId),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.AddChannelToBridgeAsync(someTalkBridge.Id, someCustomerChannelId),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.AddChannelToBridgeAsync(someTalkBridge.Id, someAgentChannel.ChannelId),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
