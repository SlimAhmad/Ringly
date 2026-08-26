using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ringly.Abstractions.Models;

namespace Ringly.Samples.WebApi;

public enum ClaimAttemptResult
{
    NotFound,
    Claimed,
    AlreadyClaimed
}

// Ties SupportController's real customer-routing flow to AgentsController's broadcast/claim
// endpoints — a gap that otherwise leaves a routed customer waiting on hold forever with no agent
// ever notified. Deliberately NOT built on ICallCenterProvider.StreamCallBroadcasts()/
// ClaimCallAsync(): those are a pure passthrough to a custom Asterisk ARI resource
// (/ari/events/claim) that only ever gets exercised by the [broadcast_test] smoke-test dialplan
// context (see docker/asterisk/config/extensions.conf's own comment) — a channel that entered a
// holding bridge via RouteToQueueAsync was never part of a real StasisBroadcast(), so claiming it
// through that resource has no defined behavior. Claim arbitration here is instead a plain,
// atomic, app-level "first writer wins" — the same mechanism, just at the WebApi layer instead of
// Asterisk's. No cleanup/expiry of abandoned entries — same scope discipline as
// InMemoryQueueRegistry, which never evicts either.
public class SupportQueueBroadcastRegistry
{
    // Row #38f — distinguishes a customer sitting in Ringly's OWN "holding" bridge (needs
    // ConnectAgentToQueueAsync's remove-and-recreate-as-mixing-bridge step, since a holding
    // bridge doesn't mix two-way audio) from one already sitting in a real, already-mixing bridge
    // an external ARI app created (e.g. Dograh's own Call Transfer tool, via
    // QueueTransferRegistrarService) — that one just needs the agent added directly
    // (ConnectAgentToBridgeAsync), never removed/recreated, since removing anything from a bridge
    // Dograh's own app still considers itself responsible for is what makes Dograh's app conclude
    // the call ended. AgentsController.PostClaimAsync branches on this per-claim, so the agent
    // console's own "Claim" button and its single claim endpoint never need to know which kind of
    // queue entry they're looking at.
    private sealed record WaitingEntry(string BridgeId, bool IsExternalBridge);

    private readonly ConcurrentDictionary<string, WaitingEntry> entryByChannelId = new();
    private readonly ConcurrentDictionary<string, byte> claimedChannelIds = new();
    private readonly Subject<CallBroadcastEvent> waitingCustomers = new();

    public IObservable<CallBroadcastEvent> StreamWaitingCustomers() => this.waitingCustomers.AsObservable();

    public void PublishWaitingCustomer(
        Guid clientId, string queueName, string channelId, string bridgeId, bool isExternalBridge = false)
    {
        this.entryByChannelId[channelId] = new WaitingEntry(bridgeId, isExternalBridge);

        this.waitingCustomers.OnNext(new CallBroadcastEvent
        {
            ChannelId = channelId,
            CallerNumber = clientId.ToString(),
            CalledExtension = queueName
        });
    }

    public ClaimAttemptResult TryClaim(string channelId, out string? bridgeId, out bool isExternalBridge)
    {
        if (!this.entryByChannelId.TryGetValue(channelId, out WaitingEntry? entry))
        {
            bridgeId = null;
            isExternalBridge = false;
            return ClaimAttemptResult.NotFound;
        }

        bridgeId = entry.BridgeId;
        isExternalBridge = entry.IsExternalBridge;

        return this.claimedChannelIds.TryAdd(channelId, 0)
            ? ClaimAttemptResult.Claimed
            : ClaimAttemptResult.AlreadyClaimed;
    }

    // Confirmed live as a real bug: TryClaim marks a channel claimed up front (correct — it must,
    // to keep the "first claim wins" check atomic against a concurrent second claim), but if
    // ConnectAgentToQueueAsync then fails (e.g. the agent's own device never answers within
    // ConnectAgentToQueueAsync's 30s Stasis-entry timeout), the claim was never released — the
    // customer became permanently unclaimable, since every subsequent attempt (even a retry by the
    // same agent) saw AlreadyClaimed forever. AgentsController.PostClaimAsync calls this in every
    // failure branch after a successful TryClaim so a failed connect attempt can be retried.
    public void ReleaseClaim(string channelId) => this.claimedChannelIds.TryRemove(channelId, out _);
}
