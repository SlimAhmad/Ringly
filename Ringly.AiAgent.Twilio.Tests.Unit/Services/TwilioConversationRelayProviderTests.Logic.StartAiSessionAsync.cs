using FluentAssertions;
using Moq;
using Ringly.AiAgent.Abstractions.Models;

namespace Ringly.AiAgent.Twilio.Tests.Unit.Services;

public partial class TwilioConversationRelayProviderTests
{
    [Fact]
    public async Task ShouldStartAiSessionAsync()
    {
        // given
        string someChannelId = GetRandomString();
        AiAgentConfig someConfig = CreateRandomAiAgentConfig();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        // when
        AiAgentSession actualSession =
            await this.provider.StartAiSessionAsync(someChannelId, someConfig);

        DateTimeOffset after = DateTimeOffset.UtcNow;

        // then
        actualSession.AiSessionId.Should().NotBeEmpty();
        actualSession.ChannelId.Should().Be(someChannelId);
        actualSession.StartedDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);

        this.twilioBrokerMock.Verify(broker =>
            broker.RedirectCallAsync(
                someChannelId,
                It.Is<string>(twiml =>
                    twiml.Contains($"{WebSocketBaseUrl}/{actualSession.AiSessionId}") &&
                    twiml.Contains("<ConversationRelay") &&
                    twiml.Contains($"voice=\"{someConfig.TtsVoice}\""))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
