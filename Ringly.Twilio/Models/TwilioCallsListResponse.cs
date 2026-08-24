using Newtonsoft.Json;

namespace Ringly.Twilio.Models;

// Twilio's List Calls resource wraps the actual array under a "calls" key rather than returning
// a bare JSON array — confirmed against Twilio's own Call resource docs.
internal sealed class TwilioCallsListResponse
{
    [JsonProperty("calls")]
    public List<TwilioCallResponse> Calls { get; set; } = [];
}
