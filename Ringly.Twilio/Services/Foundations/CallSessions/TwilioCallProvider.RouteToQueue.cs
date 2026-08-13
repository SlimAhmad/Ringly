using Ringly.Abstractions.Models;
using Ringly.Twilio.Models;
using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

public partial class TwilioCallProvider
{
    // Cold support entry point (§3.1a) — customer taps "contact support" with no active call in
    // progress. Unlike Asterisk's implementation, no queue registry lookup is needed: Twilio
    // auto-creates a conference by friendly name on first dial-in (same mechanism
    // StartCallSessionAsync already relies on), so queueName is usable directly as the
    // conference name — the customer simply joins the conference named after the queue.
    public ValueTask<CallSession> RouteToQueueAsync(Guid customerId, string queueName) =>
    TryCatch(async () =>
    {
        ValidateRouteToQueueRequest(customerId, queueName);

        SipCredentials? credentials = await this.sipCredentialsStore.RetrieveByClientIdAsync(customerId);

        if (credentials is null)
        {
            throw new NotFoundSipCredentialsException(customerId);
        }

        TwilioParticipant participant = await this.twilioBroker.AddParticipantAsync(queueName, new TwilioParticipantConfig
        {
            To = credentials.Extension,
            From = this.twilioOptions.DefaultCallerId
        });

        return new CallSession
        {
            CallSessionId = Guid.NewGuid(),
            BridgeId = queueName,
            CustomerChannelId = participant.CallSid
        };
    });
}
