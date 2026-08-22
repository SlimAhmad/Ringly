using Newtonsoft.Json;

namespace Ringly.Asterisk.Models;

internal sealed class AriChannelResponse
{
    // RESTFulSense serializes with Newtonsoft.Json — see ConfigTuple.cs for the full explanation.
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("caller")]
    public AriChannelCaller Caller { get; set; } = new();
}

internal sealed class AriChannelCaller
{
    [JsonProperty("number")]
    public string Number { get; set; } = string.Empty;
}
