namespace Ringly.Twilio.Brokers;

public class TwilioOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;

    // Twilio's REST API version segment has been stable ("2010-04-01") since the API's
    // original release — not expected to change, but kept configurable rather than hardcoded
    // inline everywhere it's used.
    public string BaseUrl { get; set; } = "https://api.twilio.com/2010-04-01";
}
