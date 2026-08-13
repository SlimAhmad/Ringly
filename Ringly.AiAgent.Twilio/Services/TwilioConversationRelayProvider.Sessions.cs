using Ringly.AiAgent.Abstractions.Models;
using Ringly.AiAgent.Twilio.Models;
using Ringly.AiAgent.Twilio.Models.Exceptions;

namespace Ringly.AiAgent.Twilio.Services;

public partial class TwilioConversationRelayProvider
{
    public ValueTask EndAiSessionAsync(Guid aiSessionId) =>
    TryCatchSession(async () =>
    {
        IConversationRelaySession session = FindSession(aiSessionId);
        await session.SendEndSessionAsync(handoffData: null);
        this.sessions.TryRemove(aiSessionId, out _);
    });

    public ValueTask EscalateToHumanAsync(Guid aiSessionId, string queueName) =>
    TryCatchSession(async () =>
    {
        ValidateQueueName(queueName);
        IConversationRelaySession session = FindSession(aiSessionId);
        await session.SendEndSessionAsync(handoffData: queueName);
        this.sessions.TryRemove(aiSessionId, out _);
    });

    public IObservable<TranscriptEvent> StreamTranscriptEvents() =>
        this.transcriptEvents;

    // Called by ConversationRelayWebSocketMiddleware once a connection under
    // {WebSocketBaseUrl}/{aiSessionId} completes its handshake — the raw socket I/O is untested
    // plumbing (same convention as row #31's brokers), everything past this point is.
    internal void RegisterSession(Guid aiSessionId, IConversationRelaySession session) =>
        this.sessions[aiSessionId] = session;

    internal void UnregisterSession(Guid aiSessionId) =>
        this.sessions.TryRemove(aiSessionId, out _);

    // The testable dispatch core: a "prompt" message publishes the caller's turn, asks the
    // app-supplied responder for a reply, sends it back over the session, and publishes the
    // agent's turn. Other inbound types ("setup"/"dtmf"/"interrupt"/"error") aren't acted on by
    // this provider today.
    internal async ValueTask HandleInboundMessageAsync(Guid aiSessionId, ConversationRelayInboundMessage message)
    {
        if (message.Type != ConversationRelayInboundMessage.PromptType ||
            string.IsNullOrEmpty(message.VoicePrompt))
        {
            return;
        }

        this.transcriptEvents.OnNext(new TranscriptEvent
        {
            AiSessionId = aiSessionId,
            Speaker = "Caller",
            Text = message.VoicePrompt,
            OccurredDate = DateTimeOffset.UtcNow
        });

        if (!this.sessions.TryGetValue(aiSessionId, out IConversationRelaySession? session))
        {
            return;
        }

        string reply = await this.aiAgentResponder.GetResponseAsync(aiSessionId, message.VoicePrompt);
        await session.SendTextAsync(reply);

        this.transcriptEvents.OnNext(new TranscriptEvent
        {
            AiSessionId = aiSessionId,
            Speaker = "Agent",
            Text = reply,
            OccurredDate = DateTimeOffset.UtcNow
        });
    }

    private IConversationRelaySession FindSession(Guid aiSessionId) =>
        this.sessions.TryGetValue(aiSessionId, out IConversationRelaySession? session)
            ? session
            : throw new AiAgentSessionNotFoundException(aiSessionId);
}
