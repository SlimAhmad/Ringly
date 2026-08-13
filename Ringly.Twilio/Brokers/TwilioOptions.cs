namespace Ringly.Twilio.Brokers;

public class TwilioOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;

    // Twilio's REST API version segment has been stable ("2010-04-01") since the API's
    // original release — not expected to change, but kept configurable rather than hardcoded
    // inline everywhere it's used.
    public string BaseUrl { get; set; } = "https://api.twilio.com/2010-04-01";

    // Twilio's Calls/Conference Participant APIs require a "From" number for every dial —
    // unlike Asterisk's channel model, there's no PBX-local identity to dial from. CallParticipant
    // (Ringly.Abstractions) has no per-call caller-ID concept, and adding one there would leak a
    // Twilio-specific requirement into the shared abstraction Asterisk doesn't need. A single
    // account-level caller ID (a Twilio-owned/verified number) is the pragmatic default most
    // apps actually use — not doc-specified, confirm before shipping.
    public string DefaultCallerId { get; set; } = string.Empty;
}
