using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
{
    [Fact]
    public async Task ShouldEscalateToQueueByCallerNumberAsync()
    {
        // given
        string someCallerNumber = GetRandomString();
        string someCallSid = GetRandomString();
        string someQueueName = GetRandomString();

        string expectedTwiml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            $"<Response><Dial><Conference>{someQueueName}</Conference></Dial></Response>";

        var expectedCallSession = new CallSession
        {
            BridgeId = someQueueName,
            CustomerChannelId = someCallSid
        };

        this.twilioBrokerMock.Setup(broker =>
            broker.RetrieveCallSidByCallerNumberAsync(someCallerNumber))
                .ReturnsAsync(someCallSid);

        this.twilioBrokerMock.Setup(broker =>
            broker.RedirectCallAsync(someCallSid, expectedTwiml))
                .Returns(ValueTask.CompletedTask);

        // when
        CallSession actualCallSession = await this.twilioCallProvider
            .EscalateToQueueByCallerNumberAsync(someCallerNumber, someQueueName);

        // then
        actualCallSession.Should().BeEquivalentTo(expectedCallSession, options =>
            options.Excluding(session => session.CallSessionId));

        actualCallSession.CallSessionId.Should().NotBeEmpty();

        this.twilioBrokerMock.Verify(broker =>
            broker.RetrieveCallSidByCallerNumberAsync(someCallerNumber),
                Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.RedirectCallAsync(someCallSid, expectedTwiml),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
