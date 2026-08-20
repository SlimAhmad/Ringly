using Ringly.Abstractions.Models;

namespace Ringly.Abstractions;

public partial interface ICallProvider
{
    ValueTask<CallSession> StartCallSessionAsync(CallParticipant partyA, CallParticipant partyB);

    // Cold support entry point (§3.1a) — customer taps "contact support" with no active call in
    // progress. NOT for escalating an already-connected call (a separate, not-yet-designed
    // method would be needed for that).
    ValueTask<CallSession> RouteToQueueAsync(Guid customerId, string queueName);

    // Bridges a claiming agent's own channel into an already-held customer's bridge/conference
    // (bridgeId is CallSession.BridgeId from the RouteToQueueAsync call that put the customer
    // there) — the counterpart that actually connects the two sides once an agent claims a
    // waiting customer, rather than leaving them on hold indefinitely.
    ValueTask<Channel> ConnectAgentToQueueAsync(string bridgeId, string agentExtension);
}
