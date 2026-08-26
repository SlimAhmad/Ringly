using Ringly.Abstractions.Models;
using Ringly.Twilio.Models;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

public partial class TwilioCallProvider
{
    // See ICallProvider.CallSessions.cs's own comment for the full reasoning (Dograh-specific,
    // Asterisk-side) — on Twilio this is functionally identical to ConnectAgentToQueueAsync
    // already: a Twilio conference always mixes every participant by default, so
    // ConnectAgentToQueueAsync never had a "remove the customer from a holding bridge" step to
    // begin with (see that method's own comment). Delegates to the same conference-add logic
    // rather than duplicating it.
    public ValueTask<AgentConnection> ConnectAgentToBridgeAsync(string bridgeId, string agentExtension) =>
    TryCatchAgentConnection(async () =>
    {
        ValidateConnectAgentToBridgeRequest(bridgeId, agentExtension);

        return await this.AddAgentToConferenceAsync(bridgeId, agentExtension);
    });

    private async ValueTask<AgentConnection> AddAgentToConferenceAsync(string conferenceName, string agentExtension)
    {
        TwilioParticipant participant = await this.twilioBroker.AddParticipantAsync(conferenceName, new TwilioParticipantConfig
        {
            To = agentExtension,
            From = this.twilioOptions.DefaultCallerId
        });

        return new AgentConnection { AgentChannelId = participant.CallSid, BridgeId = conferenceName };
    }
}
