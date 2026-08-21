using Ringly.Abstractions.Models;

namespace Ringly.Abstractions;

public partial interface ICallProvider
{
    ValueTask<CallSession> StartCallSessionAsync(CallParticipant partyA, CallParticipant partyB);

    // Cold support entry point (§3.1a) — customer taps "contact support" with no active call in
    // progress. NOT for escalating an already-connected call — see EscalateToQueueAsync below
    // for that.
    ValueTask<CallSession> RouteToQueueAsync(Guid customerId, string queueName);

    // Hands a call Ringly never originated or tracked (e.g. one an external AI agent like
    // Dograh is currently handling on its own) into one of the same queues RouteToQueueAsync
    // uses for a cold entry — keyed by the call's own provider-native channel id rather than a
    // Ringly customerId, since Ringly has no customer record for a call it didn't originate.
    ValueTask<CallSession> EscalateToQueueAsync(string channelId, string queueName);

    // Bridges a claiming agent's own channel into an already-held customer's bridge/conference
    // (bridgeId is CallSession.BridgeId, customerChannelId is CallSession.CustomerChannelId, both
    // from the RouteToQueueAsync call that put the customer there) — the counterpart that
    // actually connects the two sides once an agent claims a waiting customer, rather than
    // leaving them on hold indefinitely. Returns AgentConnection rather than a bare Channel: the
    // bridge actually carrying audio afterward isn't always the same one passed in (Asterisk's
    // implementation creates a fresh mixing bridge internally), so callers need the real one back
    // — confirmed live as a bug when it wasn't: callers kept reporting the original holding
    // bridge, which downstream consumers (e.g. call recording) then pointed at an empty bridge.
    ValueTask<AgentConnection> ConnectAgentToQueueAsync(string bridgeId, string customerChannelId, string agentExtension);
}
