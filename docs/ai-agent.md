# AI Voice Agents

`Ringly.AiAgent.Abstractions` defines the AI voice agent session contract:

```csharp
public interface IAiVoiceAgentProvider
{
    ValueTask<AiAgentSession> StartAiSessionAsync(string channelId, AiAgentConfig config);
    ValueTask EndAiSessionAsync(Guid aiSessionId);

    // Hands off into the existing queue infrastructure (ICallCenterProvider.CreateQueueAsync).
    ValueTask EscalateToHumanAsync(Guid aiSessionId, string queueName);

    IObservable<TranscriptEvent> StreamTranscriptEvents();
}
```

Models: `AiAgentConfig { string SystemPrompt, TtsVoice; List<string> EscalationTriggerPhrases }`,
`AiAgentSession { Guid AiSessionId; string ChannelId; DateTimeOffset StartedDate }`,
`TranscriptEvent { Guid AiSessionId; string Speaker; string Text; DateTimeOffset OccurredDate }`.

Only a Twilio implementation exists today (`Ringly.AiAgent.Twilio`, via Twilio
ConversationRelay). There's no Asterisk implementation — a self-built
STT→LLM→TTS pipeline over Asterisk's `externalMedia` is a deliberately deferred
stretch item (materially larger effort than everything else in this library
combined). There's also no Dograh implementation of this interface: Dograh always
originates or receives calls itself on every integration path it exposes, so it
can't be handed a call this library already established, and Ringly never creates
or tracks a Dograh session the way `AiAgentSession`/`aiSessionId` assumes — Dograh
runs as an independent Stasis application alongside Ringly's on the same Asterisk
instance instead. Escalating a Dograh call to a human queue does have real code
behind it, just not through `IAiVoiceAgentProvider` — see
[docker/README.md](../docker/README.md)'s Dograh section for the actual
bridge-add-agent flow (`QueueTransferRegistrarService` +
`ICallProvider.ConnectAgentToBridgeAsync`).

## Twilio ConversationRelay — `Ringly.AiAgent.Twilio`

Twilio owns STT/TTS/interruption handling; this package owns the WebSocket
transport, TwiML redirect, and session bookkeeping. **You supply the actual LLM
call** — that's the one seam this package doesn't fill in for you.

```csharp
dotnet add package Ringly.AiAgent.Twilio
```

### 1. Implement the LLM seam

```csharp
using Ringly.AiAgent.Twilio;

public class MyAiAgentResponder : IAiAgentResponder
{
    public async ValueTask<string> GetResponseAsync(Guid aiSessionId, string callerText)
    {
        // Call your LLM of choice here with callerText, return its reply.
        return await CallYourLlmAsync(callerText);
    }
}
```

### 2. Register everything

```csharp
services.Configure<ConversationRelayOptions>(options =>
    // The wss:// URL Twilio's <ConversationRelay> connects out to — must be publicly
    // reachable by Twilio. The per-session id is appended as a path segment automatically.
    options.WebSocketBaseUrl = "wss://your-public-host/conversationrelay");

services.AddSingleton<IAiAgentResponder, MyAiAgentResponder>();

// Registered as a concrete type — the WebSocket middleware takes
// TwilioConversationRelayProvider directly, not just the IAiVoiceAgentProvider interface.
services.AddSingleton<TwilioConversationRelayProvider>();
services.AddSingleton<IAiVoiceAgentProvider>(sp => sp.GetRequiredService<TwilioConversationRelayProvider>());
```

```csharp
public TwilioConversationRelayProvider(
    ITwilioBroker twilioBroker,        // Ringly.Twilio — reuse the same singleton
    ILoggingBroker loggingBroker,      // Ringly.Twilio.Brokers.ILoggingBroker
    IAiAgentResponder aiAgentResponder,
    IOptions<ConversationRelayOptions> options)
```

### 3. Wire the WebSocket middleware

```csharp
using Ringly.AiAgent.Twilio.Http;

var app = builder.Build();

app.UseWebSockets();           // must come first — standard ASP.NET Core requirement
app.UseTwilioConversationRelay(); // maps requests under /conversationrelay/{aiSessionId}
```

### 4. Start a session on an in-progress call

```csharp
using Ringly.AiAgent.Abstractions.Models;

AiAgentSession session = await aiVoiceAgentProvider.StartAiSessionAsync(
    channelId: callSid, // the Twilio CallSid of an already-connected call
    config: new AiAgentConfig
    {
        SystemPrompt = "You are a helpful support agent.", // used only by your own IAiAgentResponder
        TtsVoice = "Polly.Joanna-Neural",
        EscalationTriggerPhrases = ["talk to a human", "agent"]
    });
```

This redirects the call's TwiML to `<Connect><ConversationRelay>`, handing
Twilio-managed STT/TTS control of the call. As the caller speaks, Twilio posts
transcribed turns over the WebSocket; the provider calls your `IAiAgentResponder`
for each turn and streams both sides of the conversation:

```csharp
aiVoiceAgentProvider.StreamTranscriptEvents().Subscribe(transcriptEvent =>
    Console.WriteLine($"[{transcriptEvent.Speaker}] {transcriptEvent.Text}"));
```

### 5. End the session or escalate to a human

```csharp
await aiVoiceAgentProvider.EndAiSessionAsync(session.AiSessionId);

// or, detected an escalation trigger phrase / DTMF digit:
await aiVoiceAgentProvider.EscalateToHumanAsync(session.AiSessionId, queueName: "support");
```

`EscalateToHumanAsync` ends Twilio's ConversationRelay control and hands the queue
name along as handoff data; actually routing the call into a human queue from
there is your own TwiML webhook's responsibility (the `<Connect action="...">`
callback), not something this provider does automatically.
