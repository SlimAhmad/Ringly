using Newtonsoft.Json;

namespace Ringly.AiAgent.Twilio.Models;

// Confirmed field shape: "type":"end","handoffData" — docs.twilio.com/voice/conversationrelay/
// websocket-messages. Sending this ends ConversationRelay's control of the call and hands back
// to TwiML execution (the <Connect action="..."> callback URL, if configured, receives
// handoffData) — actually routing the call into a human queue from there is the consuming app's
// TwiML webhook responsibility, not this provider's; EscalateToHumanAsync only gets the handoff
// started with the target queue name attached.
internal sealed class ConversationRelayEndMessage
{
    [JsonProperty("type")]
    public string Type { get; } = "end";

    [JsonProperty("handoffData")]
    public string? HandoffData { get; set; }
}
