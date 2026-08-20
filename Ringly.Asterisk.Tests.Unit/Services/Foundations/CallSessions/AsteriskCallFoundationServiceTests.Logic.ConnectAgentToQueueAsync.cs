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
        string someAgentExtension = GetRandomString();
        Channel someChannel = CreateRandomChannel();

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"))
                .ReturnsAsync(someChannel);

        this.asteriskBrokerMock.Setup(broker =>
            broker.StreamStasisStartEvents())
                .Returns(new[] { new StasisStartEvent { ChannelId = someChannel.ChannelId } }.ToObservable());

        // when
        Channel actualChannel =
            await this.callFoundationService.ConnectAgentToQueueAsync(someBridgeId, someAgentExtension);

        // then
        actualChannel.Should().BeEquivalentTo(someChannel);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.StreamStasisStartEvents(),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.AddChannelToBridgeAsync(someBridgeId, someChannel.ChannelId),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
