using System.Text.Json.Serialization;

namespace Ringly.Asterisk.Models;

public class ConfigTuple
{
    [JsonPropertyName("attribute")]
    public string Attribute { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
