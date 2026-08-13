using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.Options;
using Ringly.AiAgent.Abstractions;
using Ringly.AiAgent.Abstractions.Models;
using Ringly.Twilio.Brokers;

namespace Ringly.AiAgent.Twilio.Services;

// §11's Twilio ConversationRelay implementation of IAiVoiceAgentProvider. Twilio owns
// STT/TTS/interruption handling; this provider owns the TwiML redirect that hands an in-progress
// call to ConversationRelay, the per-session WebSocket bookkeeping, and dispatching transcribed
// caller turns to the app-supplied IAiAgentResponder — the "you supply the LLM logic" seam §11.1
// calls for.
public partial class TwilioConversationRelayProvider : IAiVoiceAgentProvider
{
    private readonly ITwilioBroker twilioBroker;
    private readonly ILoggingBroker loggingBroker;
    private readonly IAiAgentResponder aiAgentResponder;
    private readonly ConversationRelayOptions options;
    private readonly ConcurrentDictionary<Guid, IConversationRelaySession> sessions = new();
    private readonly Subject<TranscriptEvent> transcriptEvents = new();

    public TwilioConversationRelayProvider(
        ITwilioBroker twilioBroker,
        ILoggingBroker loggingBroker,
        IAiAgentResponder aiAgentResponder,
        IOptions<ConversationRelayOptions> options)
    {
        this.twilioBroker = twilioBroker;
        this.loggingBroker = loggingBroker;
        this.aiAgentResponder = aiAgentResponder;
        this.options = options.Value;
    }

    public ValueTask<AiAgentSession> StartAiSessionAsync(string channelId, AiAgentConfig config) =>
    TryCatch(async () =>
    {
        ValidateStartAiSessionRequest(channelId, config);

        var aiSessionId = Guid.NewGuid();
        string webSocketUrl = $"{this.options.WebSocketBaseUrl.TrimEnd('/')}/{aiSessionId}";
        string twiml = ConversationRelayTwimlBuilder.Build(webSocketUrl, welcomeGreeting: string.Empty, config.TtsVoice);

        await this.twilioBroker.RedirectCallAsync(channelId, twiml);

        return new AiAgentSession
        {
            AiSessionId = aiSessionId,
            ChannelId = channelId,
            StartedDate = DateTimeOffset.UtcNow
        };
    });
}
