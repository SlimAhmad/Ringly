using Newtonsoft.Json;

namespace Ringly.Twilio.Models;

internal sealed class TwilioTaskQueueResponse
{
    [JsonProperty("sid")]
    public string Sid { get; set; } = string.Empty;

    [JsonProperty("friendly_name")]
    public string FriendlyName { get; set; } = string.Empty;
}
