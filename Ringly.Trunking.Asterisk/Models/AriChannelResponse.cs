using Newtonsoft.Json;

namespace Ringly.Trunking.Asterisk.Models;

internal sealed class AriChannelResponse
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
}
