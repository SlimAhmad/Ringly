using Ringly.Abstractions.Models;

namespace Ringly.Abstractions;

public partial interface ICallProvider
{
    ValueTask<CallSession> StartCallSessionAsync(CallParticipant partyA, CallParticipant partyB);

    // Cold support entry point (§3.1a) — customer taps "contact support" with no active call in
    // progress. NOT for escalating an already-connected call (a separate, not-yet-designed
    // method would be needed for that).
    ValueTask<CallSession> RouteToQueueAsync(Guid customerId, string queueName);
}
