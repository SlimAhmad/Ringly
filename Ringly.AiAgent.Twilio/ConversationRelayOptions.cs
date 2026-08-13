namespace Ringly.AiAgent.Twilio;

public class ConversationRelayOptions
{
    // The wss:// URL Twilio's <ConversationRelay> TwiML verb connects out to — must be publicly
    // reachable by Twilio, same reverse-proxy consideration as TwilioWebhookOptions.PublicBaseUrlOverride
    // (row #32). The per-session aiSessionId is appended as a path segment
    // (ConversationRelayWebSocketMiddleware.PathPrefix) so the WebSocket handler can correlate
    // the incoming connection to the session that requested it, without waiting on the "setup"
    // message round-trip.
    public string WebSocketBaseUrl { get; set; } = string.Empty;
}
