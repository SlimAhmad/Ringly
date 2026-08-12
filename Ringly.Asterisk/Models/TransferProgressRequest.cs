using Newtonsoft.Json;

namespace Ringly.Asterisk.Models;

internal sealed class TransferProgressRequest
{
    // RESTFulSense serializes with Newtonsoft.Json — see ConfigTuple.cs for the full explanation.
    [JsonProperty("states")]
    public string States { get; set; } = string.Empty;
}
