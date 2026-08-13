using FluentAssertions;
using Moq;
using Ringly.AiAgent.Abstractions.Models;
using Ringly.AiAgent.Twilio.Models.Exceptions;

namespace Ringly.AiAgent.Twilio.Tests.Unit.Services;

public partial class TwilioConversationRelayProviderTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnStartAiSessionIfConfigIsNullAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        AiAgentConfig nullConfig = null!;
        var nullAiAgentConfigException = new NullAiAgentConfigException();

        var expectedValidationException =
            new AiAgentSessionValidationException(nullAiAgentConfigException);

        // when
        ValueTask<AiAgentSession> startTask =
            this.provider.StartAiSessionAsync(someChannelId, nullConfig);

        AiAgentSessionValidationException actualException =
            await Assert.ThrowsAsync<AiAgentSessionValidationException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnStartAiSessionIfChannelIdIsInvalidAndLogItAsync(
        string? invalidChannelId)
    {
        // given
        AiAgentConfig someConfig = CreateRandomAiAgentConfig();
        var invalidAiAgentSessionRequestException = new InvalidAiAgentSessionRequestException();

        invalidAiAgentSessionRequestException.UpsertDataList(
            key: "channelId",
            value: "Value is required");

        var expectedValidationException =
            new AiAgentSessionValidationException(invalidAiAgentSessionRequestException);

        // when
        ValueTask<AiAgentSession> startTask =
            this.provider.StartAiSessionAsync(invalidChannelId!, someConfig);

        AiAgentSessionValidationException actualException =
            await Assert.ThrowsAsync<AiAgentSessionValidationException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
