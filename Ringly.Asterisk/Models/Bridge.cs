using Newtonsoft.Json;

namespace Ringly.Asterisk.Models;

public class Bridge
{
    // RESTFulSense serializes with Newtonsoft.Json — see ConfigTuple.cs for the full explanation.
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("bridge_type")]
    public string BridgeType { get; set; } = string.Empty;
}
