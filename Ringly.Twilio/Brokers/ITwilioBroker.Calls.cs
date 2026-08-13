using Ringly.Abstractions.Models;

namespace Ringly.Twilio.Brokers;

public partial interface ITwilioBroker
{
    ValueTask<Channel> InsertCallAsync(string to, string from, string twiml);
    ValueTask RedirectCallAsync(string callSid, string twiml);
    ValueTask HangupCallAsync(string callSid);
}
