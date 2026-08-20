using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
{
    // Bridges a claiming agent's own channel into an already-held customer's bridge — the
    // counterpart to RouteToQueueAsync that actually connects the two sides once an agent claims
    // a waiting customer. No name-based lookup step (unlike RouteToQueueAsync's IQueueRegistry
    // lookup) — bridgeId comes from the RouteToQueueAsync call that put the customer there, so an
    // unknown/expired bridgeId surfaces naturally as a dependency (validation) exception from
    // AddChannelToBridgeAsync itself, already mapped by TryCatchChannel's own catch ladder.
    public ValueTask<Channel> ConnectAgentToQueueAsync(string bridgeId, string agentExtension) =>
    TryCatchChannel(async () =>
    {
        ValidateConnectAgentToQueueRequest(bridgeId, agentExtension);

        Channel agentChannel = await this.asteriskBroker.InsertChannelAsync($"PJSIP/{agentExtension}");

        // See StartCallSessionAsync's own comment — a just-originated channel isn't biddable
        // until it actually enters the Stasis app.
        await WaitForStasisStartAsync(this.asteriskBroker, agentChannel.ChannelId);

        await this.asteriskBroker.AddChannelToBridgeAsync(bridgeId, agentChannel.ChannelId);

        return agentChannel;
    });
}
