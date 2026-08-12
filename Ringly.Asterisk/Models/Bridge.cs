using System.Text.Json.Serialization;

namespace Ringly.Asterisk.Models;

public class Bridge
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("bridge_type")]
    public string BridgeType { get; set; } = string.Empty;
}
