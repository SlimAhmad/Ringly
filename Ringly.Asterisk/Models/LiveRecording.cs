using System.Text.Json.Serialization;

namespace Ringly.Asterisk.Models;

public class LiveRecording
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}
