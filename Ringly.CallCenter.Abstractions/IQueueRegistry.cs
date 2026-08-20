using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.CallCenter.Abstractions;

// App-level persistence contract — Ringly needs to look up a queue's HoldingBridge by name to
// route a cold support request into it, but doesn't own where that gets stored; no concrete
// implementation ships from this library. RegisterAsync is called by CreateQueueAsync's
// implementations (§3.3) at creation time so RetrieveByNameAsync can find it later.
public interface IQueueRegistry
{
    // Nullable, not throwing — matches this codebase's established not-found convention
    // (e.g. IMaskingSessionStore, row #26): the store reports absence, the calling foundation
    // service decides what that means and throws its own domain exception.
    ValueTask<HoldingBridge?> RetrieveByNameAsync(string queueName);
    ValueTask RegisterAsync(HoldingBridge holdingBridge);

    // Lets a UI show/manage the set of registered queues ("departments") rather than requiring
    // callers to already know a queue's exact name up front.
    ValueTask<IReadOnlyList<HoldingBridge>> RetrieveAllAsync();

    // Removes a queue's registry record only — does not tear down the underlying Asterisk
    // bridge/Twilio conference itself (this library has no "destroy bridge" capability at all;
    // see AsteriskBroker.Bridges.cs, which only ever creates/adds-to bridges, never deletes one).
    // A caller removing a queue that still has customers on hold in it is a known, accepted gap.
    ValueTask RemoveAsync(string queueName);
}
