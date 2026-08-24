using Ringly.Abstractions.Models;
using Ringly.Twilio.Models;

namespace Ringly.Twilio.Brokers;

public partial class TwilioBroker
{
    private const string CallsRelativeUrl = "Calls.json";
    private const string CallRelativeUrlFormat = "Calls/{0}.json";

    // POST /Accounts/{AccountSid}/Calls.json — confirmed against Twilio's Call resource docs.
    public async ValueTask<Channel> InsertCallAsync(string to, string from, string twiml)
    {
        TwilioCallResponse response = await this.PostFormAsync<TwilioCallResponse>(
            CallsRelativeUrl,
            [
                new("To", to),
                new("From", from),
                new("Twiml", twiml)
            ]);

        return new Channel { ChannelId = response.Sid };
    }

    // Redirects an in-progress call to new TwiML — same Update-a-Call endpoint as HangupCallAsync,
    // different field (Twiml vs. Status).
    public async ValueTask RedirectCallAsync(string callSid, string twiml) =>
        await this.PostFormAsync(
            string.Format(CallRelativeUrlFormat, callSid),
            [new("Twiml", twiml)]);

    // Status=completed ends an already-answered/in-progress call; Status=canceled (not used
    // here) only applies to a still-ringing call — confirmed against Twilio's own OpenAPI spec
    // (call_enum_update_status: canceled, completed).
    public async ValueTask HangupCallAsync(string callSid) =>
        await this.PostFormAsync(
            string.Format(CallRelativeUrlFormat, callSid),
            [new("Status", "completed")]);

    // GET Calls.json?From=...&Status=in-progress — the only way to resolve a call SID for an
    // escalating external AI agent that can only ever supply the caller's own phone number to a
    // tool call, never the call SID itself (same gap as Asterisk ARI's channel id, confirmed for
    // Dograh specifically). Picks the first match; a genuine same-number concurrent-call
    // collision is out of scope here (mirrors AsteriskBroker's own equivalent).
    public async ValueTask<string?> RetrieveCallSidByCallerNumberAsync(string callerNumber)
    {
        TwilioCallsListResponse response = await this.GetAsync<TwilioCallsListResponse>(
            $"{CallsRelativeUrl}?From={Uri.EscapeDataString(callerNumber)}&Status=in-progress");

        return response.Calls.FirstOrDefault()?.Sid;
    }
}
