using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
{
    [Fact]
    public async Task ShouldEscalateToQueueAsync()
    {
        // given
        string someChannelId = GetRandomString();
        string someQueueName = GetRandomString();

        string expectedTwiml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            $"<Response><Dial><Conference>{someQueueName}</Conference></Dial></Response>";

        var expectedCallSession = new CallSession
        {
            BridgeId = someQueueName,
            CustomerChannelId = someChannelId
        };

        this.twilioBrokerMock.Setup(broker =>
            broker.RedirectCallAsync(someChannelId, expectedTwiml))
                .Returns(ValueTask.CompletedTask);

        // when
        CallSession actualCallSession =
            await this.twilioCallProvider.EscalateToQueueAsync(someChannelId, someQueueName);

        // then
        actualCallSession.Should().BeEquivalentTo(expectedCallSession, options =>
            options.Excluding(session => session.CallSessionId));

        actualCallSession.CallSessionId.Should().NotBeEmpty();

        this.twilioBrokerMock.Verify(broker =>
            broker.RedirectCallAsync(someChannelId, expectedTwiml),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
