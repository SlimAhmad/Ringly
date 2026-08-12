using Newtonsoft.Json;

namespace Ringly.Asterisk.Models;

public class ConfigTuple
{
    // RESTFulSense serializes with Newtonsoft.Json, not System.Text.Json — confirmed against
    // its source (JsonConvert.DeserializeObject in RESTFulApiClient.cs). A System.Text.Json
    // JsonPropertyNameAttribute here is silently ignored, sending PascalCase keys Asterisk's
    // ARI doesn't recognize (fields end up either dropped or failing field-value validation).
    [JsonProperty("attribute")]
    public string Attribute { get; set; } = string.Empty;

    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;
}
