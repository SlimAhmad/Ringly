using System.Security;
using Ringly.Abstractions.Models;

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

        string twiml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            $"<Response><Dial><Conference>{SecurityElement.Escape(queueName)}</Conference></Dial></Response>";

        await this.twilioBroker.RedirectCallAsync(channelId, twiml);

        return new CallSession
        {
            CallSessionId = Guid.NewGuid(),
            BridgeId = queueName,
            CustomerChannelId = channelId
        };
    });
}
