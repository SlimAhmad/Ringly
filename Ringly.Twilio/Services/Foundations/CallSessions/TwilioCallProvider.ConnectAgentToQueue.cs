using Ringly.Abstractions.Models;
using Ringly.Twilio.Models;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

public partial class TwilioCallProvider
{
    // Counterpart to RouteToQueueAsync — bridgeId there is really the conference name (see
    // RouteToQueueAsync's own CallSession.BridgeId = queueName), so connecting a claiming agent
    // is just dialing their extension into that same conference, the same AddParticipantAsync
    // call RouteToQueueAsync already uses for the customer side.
    public ValueTask<Channel> ConnectAgentToQueueAsync(string bridgeId, string agentExtension) =>
    TryCatchChannel(async () =>
    {
        ValidateConnectAgentToQueueRequest(bridgeId, agentExtension);

        TwilioParticipant participant = await this.twilioBroker.AddParticipantAsync(bridgeId, new TwilioParticipantConfig
        {
            To = agentExtension,
            From = this.twilioOptions.DefaultCallerId
        });

        return new Channel { ChannelId = participant.CallSid };
    });
}
