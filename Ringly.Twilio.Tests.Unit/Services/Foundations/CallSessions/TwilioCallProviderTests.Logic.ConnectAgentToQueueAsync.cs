using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Models;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
{
    [Fact]
    public async Task ShouldConnectAgentToQueueAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someCustomerChannelId = GetRandomString();
        string someAgentExtension = GetRandomString();

        var returnedParticipant = new TwilioParticipant
        {
            CallSid = GetRandomString(),
            ConferenceSid = GetRandomString()
        };

        var expectedAgentConnection = new AgentConnection
        {
            AgentChannelId = returnedParticipant.CallSid,
            BridgeId = someBridgeId
        };

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(someBridgeId, It.Is<TwilioParticipantConfig>(config =>
                config.To == someAgentExtension && config.From == DefaultCallerId)))
                    .ReturnsAsync(returnedParticipant);

        // when
        AgentConnection actualAgentConnection = await this.twilioCallProvider.ConnectAgentToQueueAsync(
            someBridgeId, someCustomerChannelId, someAgentExtension);

        // then
        actualAgentConnection.Should().BeEquivalentTo(expectedAgentConnection);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(someBridgeId, It.Is<TwilioParticipantConfig>(config =>
                config.To == someAgentExtension && config.From == DefaultCallerId)),
                    Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
