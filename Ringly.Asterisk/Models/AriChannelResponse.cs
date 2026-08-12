using System.Text.Json.Serialization;

namespace Ringly.Asterisk.Models;

internal sealed class AriChannelResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
