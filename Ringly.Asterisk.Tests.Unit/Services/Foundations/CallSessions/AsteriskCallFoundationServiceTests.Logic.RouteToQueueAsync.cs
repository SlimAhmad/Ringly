using System.Reactive.Linq;
using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Fact]
    public async Task ShouldRouteToQueueAsync()
    {
        // given
        Guid someCustomerId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        SipCredentials someCredentials = CreateRandomSipCredentials();
        HoldingBridge someHoldingBridge = CreateRandomHoldingBridge();
        Channel someChannel = CreateRandomChannel();

        var expectedCallSession = new CallSession
        {
            BridgeId = someHoldingBridge.BridgeId,
            CustomerChannelId = someChannel.ChannelId
        };

        this.sipCredentialsStoreMock.Setup(store =>
            store.RetrieveByClientIdAsync(someCustomerId))
                .ReturnsAsync(someCredentials);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(someHoldingBridge);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync($"PJSIP/{someCredentials.Extension}"))
                .ReturnsAsync(someChannel);

        this.asteriskBrokerMock.Setup(broker =>
            broker.StreamStasisStartEvents())
                .Returns(new[] { new StasisStartEvent { ChannelId = someChannel.ChannelId } }.ToObservable());

        // when
        CallSession actualCallSession =
            await this.callFoundationService.RouteToQueueAsync(someCustomerId, someQueueName);

        // then
        actualCallSession.Should().BeEquivalentTo(expectedCallSession, options =>
            options.Excluding(session => session.CallSessionId));

        actualCallSession.CallSessionId.Should().NotBeEmpty();

        this.sipCredentialsStoreMock.Verify(store =>
            store.RetrieveByClientIdAsync(someCustomerId),
                Times.Once);

        this.queueRegistryMock.Verify(registry =>
            registry.RetrieveByNameAsync(someQueueName),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync($"PJSIP/{someCredentials.Extension}"),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.AddChannelToBridgeAsync(someHoldingBridge.BridgeId, someChannel.ChannelId),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.StreamStasisStartEvents(),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
