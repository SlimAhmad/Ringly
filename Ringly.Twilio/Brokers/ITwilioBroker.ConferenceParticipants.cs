using Ringly.Twilio.Models;

namespace Ringly.Twilio.Brokers;

public partial interface ITwilioBroker
{
    ValueTask<TwilioParticipant> AddParticipantAsync(string conferenceSid, TwilioParticipantConfig config);
    ValueTask MuteParticipantAsync(string conferenceSid, string callSid, bool muted);
    ValueTask HoldParticipantAsync(string conferenceSid, string callSid, bool hold);
    ValueTask CoachParticipantAsync(string conferenceSid, string callSid, string callSidToCoach);
    ValueTask RemoveParticipantAsync(string conferenceSid, string callSid);
}
