namespace Ringly.Abstractions.Models;

public class CallSession
{
    public Guid CallSessionId { get; set; }
    public string BridgeId { get; set; } = string.Empty;
    public Guid TripId { get; set; }

    // Populated by RouteToQueueAsync (cold support entry, §3.1a) — the customer's own channel,
    // needed later for in-call actions (hold, transfer, monitor). Left empty for
    // StartCallSessionAsync's two-known-parties flow, which has no single "the customer" channel
    // in the same sense.
    public string CustomerChannelId { get; set; } = string.Empty;
}
