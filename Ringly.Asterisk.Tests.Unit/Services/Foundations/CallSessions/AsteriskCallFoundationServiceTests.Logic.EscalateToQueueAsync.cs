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
    public async Task ShouldEscalateToQueueAsync()
    {
        // given
        string someChannelId = GetRandomString();
        string someQueueName = GetRandomString();
        HoldingBridge someHoldingBridge = CreateRandomHoldingBridge();

        var expectedCallSession = new CallSession
        {
            BridgeId = someHoldingBridge.BridgeId,
            CustomerChannelId = someChannelId
        };

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(someHoldingBridge);

        this.asteriskBrokerMock.Setup(broker =>
            broker.StreamStasisStartEvents())
                .Returns(new[] { new StasisStartEvent { ChannelId = someChannelId } }.ToObservable());

        // when
        CallSession actualCallSession =
            await this.callFoundationService.EscalateToQueueAsync(someChannelId, someQueueName);

        // then
        actualCallSession.Should().BeEquivalentTo(expectedCallSession, options =>
            options.Excluding(session => session.CallSessionId));

        actualCallSession.CallSessionId.Should().NotBeEmpty();

        this.queueRegistryMock.Verify(registry =>
            registry.RetrieveByNameAsync(someQueueName),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.MoveChannelAsync(someChannelId),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.AddChannelToBridgeAsync(someHoldingBridge.BridgeId, someChannelId),
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
