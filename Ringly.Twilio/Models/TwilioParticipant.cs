using Newtonsoft.Json;

namespace Ringly.Twilio.Models;

public class TwilioParticipant
{
    [JsonProperty("call_sid")]
    public string CallSid { get; set; } = string.Empty;

    [JsonProperty("conference_sid")]
    public string ConferenceSid { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;
}
