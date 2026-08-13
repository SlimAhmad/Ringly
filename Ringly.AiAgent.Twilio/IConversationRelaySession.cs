namespace Ringly.AiAgent.Twilio;

// Thin seam over the raw WebSocket for one active ConversationRelay connection — lets
// TwilioConversationRelayProvider's dispatch/session logic be unit-tested without a real socket.
// ConversationRelayWebSocketMiddleware supplies the real implementation and registers/deregisters
// it against the provider's session registry as connections open/close.
public interface IConversationRelaySession
{
    ValueTask SendTextAsync(string text);
    ValueTask SendEndSessionAsync(string? handoffData);
}
