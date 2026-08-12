using Newtonsoft.Json;

namespace Ringly.Asterisk.Models;

// Wraps the field list for the ARI dynamic config PUT — Asterisk rejects a bare JSON array
// ("failed field value validation", silently ignoring the fields on lenient object types) and
// requires the array under a "fields" property, confirmed against the real endpoint.
internal sealed class PjsipConfigRequestBody
{
    [JsonProperty("fields")]
    public IReadOnlyList<ConfigTuple> Fields { get; set; } = [];
}
