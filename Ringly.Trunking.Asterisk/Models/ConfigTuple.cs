using Newtonsoft.Json;

namespace Ringly.Trunking.Asterisk.Models;

// RESTFulSense serializes with Newtonsoft.Json, not System.Text.Json — a JsonPropertyName
// attribute here would be silently ignored. See Ringly.Asterisk.Models.ConfigTuple for the
// full story (row #21).
internal sealed class ConfigTuple
{
    [JsonProperty("attribute")]
    public string Attribute { get; set; } = string.Empty;

    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;
}
