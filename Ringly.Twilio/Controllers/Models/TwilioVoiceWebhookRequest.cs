namespace Ringly.Twilio.Controllers.Models;

// Standard Twilio Voice webhook / status callback POST parameters (form-urlencoded) — property
// names match Twilio's field names so ASP.NET Core's default [FromForm] binder (case-insensitive)
// picks them up without extra attributes. Only the fields this row's mapping to CallEvent
// actually uses; Twilio sends more (Direction, ApiVersion, ForwardedFrom, CallerName, etc.) that
// callers needing them can bind separately.
public class TwilioVoiceWebhookRequest
{
    public string CallSid { get; set; } = string.Empty;
    public string AccountSid { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string CallStatus { get; set; } = string.Empty;
}
