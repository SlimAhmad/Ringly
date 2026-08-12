using Newtonsoft.Json;

namespace Ringly.Asterisk.Models;

public class LiveRecording
{
    // RESTFulSense serializes with Newtonsoft.Json — see ConfigTuple.cs for the full explanation.
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;
}
