using Newtonsoft.Json;

namespace Ringly.Trunking.Asterisk.Models;

// The ARI dynamic config PUT requires the field list wrapped in "fields" — a bare array is
// either silently ignored or rejected outright. Confirmed against the real endpoint (row #21).
internal sealed class PjsipConfigRequestBody
{
    [JsonProperty("fields")]
    public IReadOnlyList<ConfigTuple> Fields { get; set; } = [];
}
