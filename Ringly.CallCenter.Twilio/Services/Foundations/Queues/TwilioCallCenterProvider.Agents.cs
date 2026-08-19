using System.Reactive.Linq;
using Ringly.Abstractions.Models;

namespace Ringly.CallCenter.Twilio.Services.Foundations.Queues;

public partial class TwilioCallCenterProvider
{
    // Unlike StreamTransferRequests (a confirmed platform gap — see
    // TwilioCallCenterProvider.Transfers.cs's own comment), Twilio's TaskRouter genuinely has
    // matching primitives for all three of these (Reservation accept/reject for claim, Worker
    // Activity for availability, TaskRouter/Voice event webhooks for broadcasts) — this just isn't
    // built yet. Stubbed with NotSupportedException/an empty stream rather than a real
    // implementation, which would need new ITwilioBroker methods (reservation update, worker
    // activity update) this broker doesn't have yet. Tracked as separate follow-up work, not
    // attempted here.
    public IObservable<CallBroadcastEvent> StreamCallBroadcasts() =>
        Observable.Empty<CallBroadcastEvent>();

    public ValueTask<ClaimResult> ClaimCallAsync(string channelId, string agentAppName) =>
        throw new NotSupportedException(
            "ClaimCallAsync is not yet implemented for Twilio — needs a TaskRouter Reservation " +
            "accept/reject broker method.");

    public ValueTask SetAgentAvailabilityAsync(string agentAppName, bool isAvailable) =>
        throw new NotSupportedException(
            "SetAgentAvailabilityAsync is not yet implemented for Twilio — needs a TaskRouter " +
            "Worker Activity broker method.");
}
