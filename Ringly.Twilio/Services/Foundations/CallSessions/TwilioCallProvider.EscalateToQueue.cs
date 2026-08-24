using System.Security;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

public partial class TwilioCallProvider
{
    // Counterpart to RouteToQueueAsync for a call Ringly never originated (e.g. one Twilio's own
    // ConversationRelay or an external AI agent is currently handling) — redirects the live
    // call's TwiML into the same conference RouteToQueueAsync/ConnectAgentToQueueAsync already
    // use for that queueName, the same RedirectCallAsync mechanism
    // TwilioConversationRelayProvider.StartAiSessionAsync uses to hand a call off mid-call.
    public ValueTask<CallSession> EscalateToQueueAsync(string channelId, string queueName) =>
    TryCatch(async () =>
    {
        ValidateEscalateToQueueRequest(channelId, queueName);

        return await EscalateChannelToQueueAsync(channelId, queueName);
    });

    // Same escalation, resolved from the caller's own phone number instead of a call SID — for an
    // external AI agent whose tool-calling mechanism never exposes the live call SID to the tool
    // itself, only caller_number (same gap confirmed for Dograh's own ARI integration; kept here
    // too so both ICallProvider implementations expose the same contract).
    public ValueTask<CallSession> EscalateToQueueByCallerNumberAsync(string callerNumber, string queueName) =>
    TryCatch(async () =>
    {
        ValidateEscalateToQueueByCallerNumberRequest(callerNumber, queueName);

        string? callSid = await this.twilioBroker.RetrieveCallSidByCallerNumberAsync(callerNumber);

        if (callSid is null)
        {
            throw new NotFoundChannelException(callerNumber);
        }

        return await EscalateChannelToQueueAsync(callSid, queueName);
    });

    private async ValueTask<CallSession> EscalateChannelToQueueAsync(string channelId, string queueName)
    {
        string twiml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            $"<Response><Dial><Conference>{SecurityElement.Escape(queueName)}</Conference></Dial></Response>";

        await this.twilioBroker.RedirectCallAsync(channelId, twiml);

        return new CallSession
        {
            CallSessionId = Guid.NewGuid(),
            BridgeId = queueName,
            CustomerChannelId = channelId
        };
    }
}
