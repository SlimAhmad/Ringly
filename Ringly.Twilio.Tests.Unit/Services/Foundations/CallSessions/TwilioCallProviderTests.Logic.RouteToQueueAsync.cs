using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Models;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
{
    [Fact]
    public async Task ShouldRouteToQueueAsync()
    {
        // given
        Guid someCustomerId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        SipCredentials someCredentials = CreateRandomSipCredentials();

        var returnedParticipant = new TwilioParticipant
        {
            CallSid = GetRandomString(),
            ConferenceSid = GetRandomString()
        };

        var expectedCallSession = new CallSession
        {
            BridgeId = someQueueName,
            CustomerChannelId = returnedParticipant.CallSid
        };

        this.sipCredentialsStoreMock.Setup(store =>
            store.RetrieveByClientIdAsync(someCustomerId))
                .ReturnsAsync(someCredentials);

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(someQueueName, It.Is<TwilioParticipantConfig>(config =>
                config.To == someCredentials.Extension && config.From == DefaultCallerId)))
                    .ReturnsAsync(returnedParticipant);

        // when
        CallSession actualCallSession =
            await this.twilioCallProvider.RouteToQueueAsync(someCustomerId, someQueueName);

        // then
        actualCallSession.Should().BeEquivalentTo(expectedCallSession, options =>
            options.Excluding(session => session.CallSessionId));

        actualCallSession.CallSessionId.Should().NotBeEmpty();

        this.sipCredentialsStoreMock.Verify(store =>
            store.RetrieveByClientIdAsync(someCustomerId),
                Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(someQueueName, It.Is<TwilioParticipantConfig>(config =>
                config.To == someCredentials.Extension && config.From == DefaultCallerId)),
                    Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
