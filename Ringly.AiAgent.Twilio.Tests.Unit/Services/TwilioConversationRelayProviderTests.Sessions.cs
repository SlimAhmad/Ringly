using FluentAssertions;
using Moq;
using Ringly.AiAgent.Abstractions.Models;
using Ringly.AiAgent.Twilio.Models;
using Ringly.AiAgent.Twilio.Models.Exceptions;

namespace Ringly.AiAgent.Twilio.Tests.Unit.Services;

public partial class TwilioConversationRelayProviderTests
{
    [Fact]
    public async Task ShouldEndAiSessionAsync()
    {
        // given
        var aiSessionId = Guid.NewGuid();
        var sessionMock = new Mock<IConversationRelaySession>();
        this.provider.RegisterSession(aiSessionId, sessionMock.Object);

        // when
        await this.provider.EndAiSessionAsync(aiSessionId);

        // then
        sessionMock.Verify(session =>
            session.SendEndSessionAsync(null),
                Times.Once);

        sessionMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnEndAiSessionIfSessionNotFoundAndLogItAsync()
    {
        // given
        var unknownAiSessionId = Guid.NewGuid();
        var aiAgentSessionNotFoundException = new AiAgentSessionNotFoundException(unknownAiSessionId);

        var expectedValidationException =
            new AiAgentSessionValidationException(aiAgentSessionNotFoundException);

        // when
        ValueTask endTask = this.provider.EndAiSessionAsync(unknownAiSessionId);

        AiAgentSessionValidationException actualException =
            await Assert.ThrowsAsync<AiAgentSessionValidationException>(endTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldEscalateToHumanAsync()
    {
        // given
        var aiSessionId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        var sessionMock = new Mock<IConversationRelaySession>();
        this.provider.RegisterSession(aiSessionId, sessionMock.Object);

        // when
        await this.provider.EscalateToHumanAsync(aiSessionId, someQueueName);

        // then
        sessionMock.Verify(session =>
            session.SendEndSessionAsync(someQueueName),
                Times.Once);

        sessionMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnEscalateToHumanIfQueueNameIsInvalidAndLogItAsync(
        string? invalidQueueName)
    {
        // given
        var aiSessionId = Guid.NewGuid();
        var invalidAiAgentSessionRequestException = new InvalidAiAgentSessionRequestException();

        invalidAiAgentSessionRequestException.UpsertDataList(
            key: "queueName",
            value: "Value is required");

        var expectedValidationException =
            new AiAgentSessionValidationException(invalidAiAgentSessionRequestException);

        // when
        ValueTask escalateTask = this.provider.EscalateToHumanAsync(aiSessionId, invalidQueueName!);

        AiAgentSessionValidationException actualException =
            await Assert.ThrowsAsync<AiAgentSessionValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldNotEmitTranscriptEventOnNonPromptMessageAsync()
    {
        // given
        var aiSessionId = Guid.NewGuid();
        var receivedEvents = new List<TranscriptEvent>();
        using IDisposable subscription = this.provider.StreamTranscriptEvents().Subscribe(receivedEvents.Add);

        var setupMessage = new ConversationRelayInboundMessage
        {
            Type = ConversationRelayInboundMessage.SetupType,
            CallSid = GetRandomString()
        };

        // when
        await this.provider.HandleInboundMessageAsync(aiSessionId, setupMessage);

        // then
        receivedEvents.Should().BeEmpty();
        this.aiAgentResponderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldPublishCallerAndAgentTranscriptEventsOnPromptMessageAsync()
    {
        // given
        var aiSessionId = Guid.NewGuid();
        string callerText = GetRandomString();
        string agentReply = GetRandomString();
        var sessionMock = new Mock<IConversationRelaySession>();
        this.provider.RegisterSession(aiSessionId, sessionMock.Object);

        this.aiAgentResponderMock.Setup(responder =>
            responder.GetResponseAsync(aiSessionId, callerText))
                .ReturnsAsync(agentReply);

        var receivedEvents = new List<TranscriptEvent>();
        using IDisposable subscription = this.provider.StreamTranscriptEvents().Subscribe(receivedEvents.Add);

        var promptMessage = new ConversationRelayInboundMessage
        {
            Type = ConversationRelayInboundMessage.PromptType,
            VoicePrompt = callerText,
            Last = true
        };

        // when
        await this.provider.HandleInboundMessageAsync(aiSessionId, promptMessage);

        // then
        receivedEvents.Should().HaveCount(2);
        receivedEvents[0].Speaker.Should().Be("Caller");
        receivedEvents[0].Text.Should().Be(callerText);
        receivedEvents[1].Speaker.Should().Be("Agent");
        receivedEvents[1].Text.Should().Be(agentReply);
        receivedEvents.Should().OnlyContain(transcriptEvent => transcriptEvent.AiSessionId == aiSessionId);

        this.aiAgentResponderMock.Verify(responder =>
            responder.GetResponseAsync(aiSessionId, callerText),
                Times.Once);

        sessionMock.Verify(session =>
            session.SendTextAsync(agentReply),
                Times.Once);

        this.aiAgentResponderMock.VerifyNoOtherCalls();
        sessionMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
