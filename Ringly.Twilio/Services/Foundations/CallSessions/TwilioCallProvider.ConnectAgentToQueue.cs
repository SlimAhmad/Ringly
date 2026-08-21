using Ringly.Abstractions.Models;
using Ringly.Twilio.Models;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

public partial class TwilioCallProvider
{
    // Counterpart to RouteToQueueAsync — bridgeId there is really the conference name (see
    // RouteToQueueAsync's own CallSession.BridgeId = queueName), so connecting a claiming agent
    // is just dialing their extension into that same conference, the same AddParticipantAsync
    // call RouteToQueueAsync already uses for the customer side. customerChannelId (needed on
    // the Asterisk side to move the customer out of a non-mixing holding bridge — see that
    // provider's own comment) isn't needed here: Twilio conferences always mix every participant
    // by default, no separate "holding" bridge type exists to work around.
    public ValueTask<AgentConnection> ConnectAgentToQueueAsync(
        string bridgeId, string customerChannelId, string agentExtension) =>
    TryCatchAgentConnection(async () =>
    {
        ValidateConnectAgentToQueueRequest(bridgeId, customerChannelId, agentExtension);

        TwilioParticipant participant = await this.twilioBroker.AddParticipantAsync(bridgeId, new TwilioParticipantConfig
        {
            To = agentExtension,
            From = this.twilioOptions.DefaultCallerId
        });

        return new AgentConnection { AgentChannelId = participant.CallSid, BridgeId = bridgeId };
    });
}
