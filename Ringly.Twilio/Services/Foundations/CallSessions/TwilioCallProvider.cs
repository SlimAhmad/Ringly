using Microsoft.Extensions.Options;
using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Brokers;
using Ringly.Twilio.Models;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

// §9's "same contract, different internals" — the Twilio counterpart to
// Ringly.Asterisk.AsteriskCallFoundationService. Asterisk bridges channels into a mixing
// bridge; Twilio's equivalent primitive is a Conference (§9's comparison table: "Largely
// automatic: TaskRouter's conference instruction connects agent, waits for answer, moves
// caller in"). Dialing a participant into a conference by friendly name auto-creates that
// conference if it doesn't already exist — confirmed against Twilio's Conference Participant
// docs — so there's no separate "create conference" step needed.
public partial class TwilioCallProvider : ICallProvider
{
    private readonly ITwilioBroker twilioBroker;
    private readonly ISipCredentialsStore sipCredentialsStore;
    private readonly ILoggingBroker loggingBroker;
    private readonly TwilioOptions twilioOptions;

    public TwilioCallProvider(
        ITwilioBroker twilioBroker,
        ISipCredentialsStore sipCredentialsStore,
        ILoggingBroker loggingBroker,
        IOptions<TwilioOptions> twilioOptions)
    {
        this.twilioBroker = twilioBroker;
        this.sipCredentialsStore = sipCredentialsStore;
        this.loggingBroker = loggingBroker;
        this.twilioOptions = twilioOptions.Value;
    }

    public ValueTask<CallSession> StartCallSessionAsync(CallParticipant partyA, CallParticipant partyB) =>
    TryCatch(async () =>
    {
        ValidateCallParticipant(partyA);
        ValidateCallParticipant(partyB);

        string conferenceName = $"ringly-{Guid.NewGuid():N}";

        await this.twilioBroker.AddParticipantAsync(conferenceName, new TwilioParticipantConfig
        {
            To = partyA.SipExtension,
            From = this.twilioOptions.DefaultCallerId
        });

        await this.twilioBroker.AddParticipantAsync(conferenceName, new TwilioParticipantConfig
        {
            To = partyB.SipExtension,
            From = this.twilioOptions.DefaultCallerId
        });

        return new CallSession
        {
            CallSessionId = Guid.NewGuid(),
            BridgeId = conferenceName
        };
    });
}
