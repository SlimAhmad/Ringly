using Newtonsoft.Json;

namespace Ringly.Trunking.Asterisk.Models;

internal sealed class AriChannelListItem
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}
