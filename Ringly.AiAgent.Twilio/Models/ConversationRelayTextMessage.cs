using Newtonsoft.Json;

namespace Ringly.AiAgent.Twilio.Models;

// Confirmed field shape: "type":"text","token","last" (+"interruptible"/"preemptible"/"lang",
// not used here) — docs.twilio.com/voice/conversationrelay/websocket-messages. Twilio recommends
// streaming tokens as they're generated with last:false, then a final last:true; this provider
// sends the whole response as a single last:true token (streaming the LLM's own output is the
// consuming app's concern, one layer up from this transport).
internal sealed class ConversationRelayTextMessage
{
    [JsonProperty("type")]
    public string Type { get; } = "text";

    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;

    [JsonProperty("last")]
    public bool Last { get; set; } = true;
}
