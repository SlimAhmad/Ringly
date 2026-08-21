using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
{
    // Bridges a claiming agent's own channel with an already-held customer — the counterpart to
    // RouteToQueueAsync that actually connects the two sides once an agent claims a waiting
    // customer. No name-based lookup step (unlike RouteToQueueAsync's IQueueRegistry lookup) —
    // bridgeId/customerChannelId come from the RouteToQueueAsync call that put the customer
    // there, so an unknown/expired bridgeId surfaces naturally as a dependency (validation)
    // exception from AddChannelToBridgeAsync/RemoveChannelFromBridgeAsync themselves, already
    // mapped by TryCatchChannel's own catch ladder.
    public ValueTask<AgentConnection> ConnectAgentToQueueAsync(
        string bridgeId, string customerChannelId, string agentExtension) =>
    TryCatchAgentConnection(async () =>
    {
        ValidateConnectAgentToQueueRequest(bridgeId, customerChannelId, agentExtension);

        Channel agentChannel = await this.asteriskBroker.InsertChannelAsync($"PJSIP/{agentExtension}");

        // See StartCallSessionAsync's own comment — a just-originated channel isn't biddable
        // until it actually enters the Stasis app.
        await WaitForStasisStartAsync(this.asteriskBroker, agentChannel.ChannelId);

        // A queue's bridge (RouteToQueueAsync/CreateQueueAsync) is Asterisk's "holding" bridge
        // type, built for a customer to wait with music-on-hold — confirmed live it does NOT mix
        // two-way audio between participants the way StartCallSessionAsync's "mixing" bridge does
        // (agent + customer both showed "connected" with zero audio flowing either direction).
        // Moving the customer into a fresh mixing bridge alongside the agent, rather than just
        // adding the agent to the existing holding bridge, is what actually lets them talk — and
        // is why the *new* bridge id, not the one passed in, is what callers need back.
        Bridge talkBridge = await this.asteriskBroker.InsertBridgeAsync(MixingBridgeType);
        await this.asteriskBroker.RemoveChannelFromBridgeAsync(bridgeId, customerChannelId);
        await this.asteriskBroker.AddChannelToBridgeAsync(talkBridge.Id, customerChannelId);
        await this.asteriskBroker.AddChannelToBridgeAsync(talkBridge.Id, agentChannel.ChannelId);

        return new AgentConnection { AgentChannelId = agentChannel.ChannelId, BridgeId = talkBridge.Id };
    });
}
