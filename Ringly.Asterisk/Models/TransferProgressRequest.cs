using System.Text.Json.Serialization;

namespace Ringly.Asterisk.Models;

internal sealed class TransferProgressRequest
{
    [JsonPropertyName("states")]
    public string States { get; set; } = string.Empty;
}
