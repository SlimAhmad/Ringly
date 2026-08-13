using Ringly.Twilio.Models;

namespace Ringly.Twilio.Brokers;

public partial class TwilioBroker
{
    private const string ParticipantsRelativeUrlFormat = "Conferences/{0}/Participants.json";
    private const string ParticipantRelativeUrlFormat = "Conferences/{0}/Participants/{1}.json";

    // POST /Accounts/{AccountSid}/Conferences/{ConferenceSid}/Participants.json — dials {To}
    // into the conference. Confirmed against Twilio's Conference Participant resource docs.
    public async ValueTask<TwilioParticipant> AddParticipantAsync(string conferenceSid, TwilioParticipantConfig config) =>
        await this.PostFormAsync<TwilioParticipant>(
            string.Format(ParticipantsRelativeUrlFormat, conferenceSid),
            [
                new("To", config.To),
                new("From", config.From),
                new("Muted", FormBool(config.Muted)),
                new("Hold", FormBool(config.Hold)),
                new("Coaching", FormBool(config.Coaching)),
                new("CallSidToCoach", config.CallSidToCoach),
                new("EarlyMedia", FormBool(config.EarlyMedia)),
                new("StartConferenceOnEnter", FormBool(config.StartConferenceOnEnter)),
                new("EndConferenceOnExit", FormBool(config.EndConferenceOnExit))
            ]);

    // §5.7-equivalent monitor primitive on Twilio's side — Conference native muting, no custom
    // bridge/snoop surgery needed (see §9's Asterisk-vs-Twilio comparison table).
    public async ValueTask MuteParticipantAsync(string conferenceSid, string callSid, bool muted) =>
        await this.PostFormAsync(
            string.Format(ParticipantRelativeUrlFormat, conferenceSid, callSid),
            [new("Muted", FormBool(muted))]);

    public async ValueTask HoldParticipantAsync(string conferenceSid, string callSid, bool hold) =>
        await this.PostFormAsync(
            string.Format(ParticipantRelativeUrlFormat, conferenceSid, callSid),
            [new("Hold", FormBool(hold))]);

    // §5.7-equivalent whisper primitive — Conference native coaching, no snoopChannel
    // equivalent needed (see §9's comparison table).
    public async ValueTask CoachParticipantAsync(string conferenceSid, string callSid, string callSidToCoach) =>
        await this.PostFormAsync(
            string.Format(ParticipantRelativeUrlFormat, conferenceSid, callSid),
            [
                new("Coaching", "true"),
                new("CallSidToCoach", callSidToCoach)
            ]);

    public async ValueTask RemoveParticipantAsync(string conferenceSid, string callSid) =>
        await this.DeleteAsync(string.Format(ParticipantRelativeUrlFormat, conferenceSid, callSid));
}
