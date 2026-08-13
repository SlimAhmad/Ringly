namespace Ringly.AiAgent.Twilio;

// The "you supply the LLM logic" seam §11.1/§11.2 calls for — ConversationRelay handles
// STT/TTS/interruption itself and hands this provider plain transcribed caller text; this
// provider owns the WebSocket transport and session bookkeeping, but the actual LLM call is the
// consuming app's responsibility, registered via DI.
public interface IAiAgentResponder
{
    ValueTask<string> GetResponseAsync(Guid aiSessionId, string callerText);
}
