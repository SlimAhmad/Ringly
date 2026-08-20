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
    private readonly ConcurrentDictionary<string, string> bridgeIdByChannelId = new();
    private readonly ConcurrentDictionary<string, byte> claimedChannelIds = new();
    private readonly Subject<CallBroadcastEvent> waitingCustomers = new();

    public IObservable<CallBroadcastEvent> StreamWaitingCustomers() => this.waitingCustomers.AsObservable();

    public void PublishWaitingCustomer(Guid clientId, string queueName, string channelId, string bridgeId)
    {
        this.bridgeIdByChannelId[channelId] = bridgeId;

        this.waitingCustomers.OnNext(new CallBroadcastEvent
        {
            ChannelId = channelId,
            CallerNumber = clientId.ToString(),
            CalledExtension = queueName
        });
    }

    public ClaimAttemptResult TryClaim(string channelId, out string? bridgeId)
    {
        if (!this.bridgeIdByChannelId.TryGetValue(channelId, out bridgeId))
        {
            return ClaimAttemptResult.NotFound;
        }

        return this.claimedChannelIds.TryAdd(channelId, 0)
            ? ClaimAttemptResult.Claimed
            : ClaimAttemptResult.AlreadyClaimed;
    }
}
