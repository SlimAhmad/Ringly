using Newtonsoft.Json;

namespace Ringly.Twilio.Models;

// Twilio's JSON responses use snake_case field names. RESTFulSense serializes/deserializes
// with Newtonsoft.Json (see Ringly.Asterisk.Models.ConfigTuple for the full story on why that
// matters, row #21) — [JsonProperty] here, not System.Text.Json's [JsonPropertyName].
internal sealed class TwilioCallResponse
{
    [JsonProperty("sid")]
    public string Sid { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("to")]
    public string To { get; set; } = string.Empty;

    [JsonProperty("from")]
    public string From { get; set; } = string.Empty;
}
