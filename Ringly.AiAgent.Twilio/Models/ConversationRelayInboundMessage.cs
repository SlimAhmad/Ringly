using Newtonsoft.Json;

namespace Ringly.AiAgent.Twilio.Models;

// Twilio's ConversationRelay WebSocket protocol — confirmed against Twilio's own
// "Getting and sending WebSocket messages" reference (docs.twilio.com/voice/conversationrelay/
// websocket-messages), not guessed. One flat DTO for every inbound message shape this provider
// acts on ("setup"/"prompt"/"dtmf"; "interrupt" and "error" are received but not currently
// handled); Type is the discriminator, fields not relevant to a given type are simply absent
// from the JSON and left at their default.
public class ConversationRelayInboundMessage
{
    public const string SetupType = "setup";
    public const string PromptType = "prompt";
    public const string DtmfType = "dtmf";

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    // Only present on "setup".
    [JsonProperty("callSid")]
    public string? CallSid { get; set; }

    // Only present on "prompt" — the transcribed caller speech for this turn. Twilio's field
    // name is "voicePrompt", not "text".
    [JsonProperty("voicePrompt")]
    public string? VoicePrompt { get; set; }

    // Only present on "prompt" — true once the caller has finished this turn.
    [JsonProperty("last")]
    public bool Last { get; set; }

    // Only present on "dtmf". Twilio's field name is "digit", not "dtmf".
    [JsonProperty("digit")]
    public string? Digit { get; set; }
}
