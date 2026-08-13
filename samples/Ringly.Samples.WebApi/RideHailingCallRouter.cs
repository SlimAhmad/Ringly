using System.Collections.Concurrent;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Models;

namespace Ringly.Samples.WebApi;

// Bridges calls a client dials directly (e.g. samples/Ringly.Samples.Maui registering as "1000"
// and calling "1001") — docker/asterisk/config/extensions.conf's [ride_hailing] context hands
// every such call to Stasis(ride_hailing_app, ${EXTEN}), which without a listener here just
// leaves the caller parked with nothing happening (confirmed: no ringing, no decline, no
// timeout). StartCallSessionAsync/RouteToQueueAsync don't need this router — they originate and
// bridge both their own channels directly via the broker's HTTP API, with no dependency on
// Stasis events at all.
public class RideHailingCallRouter : BackgroundService
{
    private const string MixingBridgeType = "mixing";

    private readonly IAsteriskBroker asteriskBroker;
    private readonly ILogger<RideHailingCallRouter> logger;

    // Tracks channels this router originated itself (the callee leg) so their own StasisStart —
    // which looks identical to any other entry at a glance — gets bridged in rather than
    // mistaken for a second, unrelated client-dialed call.
    private readonly ConcurrentDictionary<string, string> pendingBridgeIdByChannelId = new();

    public RideHailingCallRouter(IAsteriskBroker asteriskBroker, ILogger<RideHailingCallRouter> logger)
    {
        this.asteriskBroker = asteriskBroker;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IDisposable subscription = this.asteriskBroker.StreamStasisStartEvents()
            .Subscribe(stasisStartEvent => this.OnStasisStart(stasisStartEvent));

        stoppingToken.Register(subscription.Dispose);

        return Task.CompletedTask;
    }

    private async void OnStasisStart(StasisStartEvent stasisStartEvent)
    {
        try
        {
            await this.HandleStasisStartAsync(stasisStartEvent);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to route Stasis-dialed call for channel {ChannelId}",
                stasisStartEvent.ChannelId);
        }
    }

    private async Task HandleStasisStartAsync(StasisStartEvent stasisStartEvent)
    {
        if (this.pendingBridgeIdByChannelId.TryRemove(stasisStartEvent.ChannelId, out string? bridgeId))
        {
            await this.asteriskBroker.AnswerChannelAsync(stasisStartEvent.ChannelId);
            await this.asteriskBroker.AddChannelToBridgeAsync(bridgeId, stasisStartEvent.ChannelId);
            return;
        }

        if (stasisStartEvent.Args.Count == 0)
        {
            return;
        }

        string targetExtension = stasisStartEvent.Args[0];

        await this.asteriskBroker.AnswerChannelAsync(stasisStartEvent.ChannelId);
        Bridge bridge = await this.asteriskBroker.InsertBridgeAsync(MixingBridgeType);
        await this.asteriskBroker.AddChannelToBridgeAsync(bridge.Id, stasisStartEvent.ChannelId);

        Channel targetChannel = await this.asteriskBroker.InsertChannelAsync($"PJSIP/{targetExtension}");
        this.pendingBridgeIdByChannelId[targetChannel.ChannelId] = bridge.Id;
    }
}
