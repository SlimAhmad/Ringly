using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
{
    // See ICallProvider.CallSessions.cs's own comment for the full reasoning — this is
    // deliberately a pure "add", never a remove/recreate, unlike ConnectAgentToQueueAsync.
    public ValueTask<AgentConnection> ConnectAgentToBridgeAsync(string bridgeId, string agentExtension) =>
    TryCatchAgentConnection(async () =>
    {
        ValidateConnectAgentToBridgeRequest(bridgeId, agentExtension);

        Channel agentChannel = await this.asteriskBroker.InsertChannelAsync($"PJSIP/{agentExtension}");

        // See StartCallSessionAsync's own comment — a just-originated channel isn't biddable
        // until it actually enters the Stasis app.
        await WaitForStasisStartAsync(this.asteriskBroker, agentChannel.ChannelId);

        // Confirmed live as a real bug: this bridge's own MOH (started by
        // QueueTransferRegistrarService while waiting for a claim, since it's never a "holding"
        // bridge Asterisk auto-plays MOH on) keeps playing to every participant, including the
        // newly-joined agent, until the whole bridge is eventually torn down — nothing ever
        // stopped it just because a real agent joined. Stopped explicitly, before adding the
        // agent, the same way a customer's holding bridge would naturally stop announcing once
        // someone answers.
        await this.asteriskBroker.StopMusicOnHoldAsync(bridgeId);

        await this.asteriskBroker.AddChannelToBridgeAsync(bridgeId, agentChannel.ChannelId);

        return new AgentConnection { AgentChannelId = agentChannel.ChannelId, BridgeId = bridgeId };
    });
}
