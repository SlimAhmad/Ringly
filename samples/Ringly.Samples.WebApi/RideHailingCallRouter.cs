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
    private const string UpChannelState = "Up";

    private readonly IAsteriskBroker asteriskBroker;
    private readonly ILogger<RideHailingCallRouter> logger;

    // Tracks channels this router originated itself (the callee leg) so the caller's leg isn't
    // mistaken for one, and so the callee leg only gets bridged once it's genuinely answered
    // (see HandleChannelStateChangeAsync) — not on its own StasisStart, which fires the instant
    // Asterisk creates the channel, well before the real device has rung, let alone answered.
    private readonly ConcurrentDictionary<string, string> pendingBridgeIdByChannelId = new();

    public RideHailingCallRouter(IAsteriskBroker asteriskBroker, ILogger<RideHailingCallRouter> logger)
    {
        this.asteriskBroker = asteriskBroker;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IDisposable stasisSubscription = this.asteriskBroker.StreamStasisStartEvents()
            .Subscribe(stasisStartEvent => this.OnStasisStart(stasisStartEvent));

        IDisposable stateChangeSubscription = this.asteriskBroker.StreamChannelStateChangeEvents()
            .Subscribe(stateChangeEvent => this.OnChannelStateChange(stateChangeEvent));

        stoppingToken.Register(() =>
        {
            stasisSubscription.Dispose();
            stateChangeSubscription.Dispose();
        });

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

    private async void OnChannelStateChange(ChannelStateChangeEvent stateChangeEvent)
    {
        try
        {
            await this.HandleChannelStateChangeAsync(stateChangeEvent);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to bridge answered channel {ChannelId}",
                stateChangeEvent.ChannelId);
        }
    }

    private async Task HandleStasisStartAsync(StasisStartEvent stasisStartEvent)
    {
        if (this.pendingBridgeIdByChannelId.ContainsKey(stasisStartEvent.ChannelId))
        {
            // Our own originated (callee) leg entering Stasis — do nothing here. Bridging it
            // now (even without an explicit answer) was confirmed to make Asterisk treat a
            // mixing bridge join as an implicit answer, which is what caused every call to
            // "answer" within 60-160ms of being dialed — before the callee's real device had
            // even rung. Waiting for its ChannelStateChange to "Up" instead means the callee's
            // own client genuinely has to answer first.
            return;
        }

        if (stasisStartEvent.Args.Count == 0)
        {
            // Not a client-dialed entry into the ride_hailing dialplan (e.g. a leg
            // Ringly.Samples.WebApi originated itself via StartCallSessionAsync/RouteToQueueAsync,
            // which bridges its own channels directly and doesn't need this router).
            return;
        }

        string targetExtension = stasisStartEvent.Args[0];

        await this.asteriskBroker.AnswerChannelAsync(stasisStartEvent.ChannelId);
        Bridge bridge = await this.asteriskBroker.InsertBridgeAsync(MixingBridgeType);
        await this.asteriskBroker.AddChannelToBridgeAsync(bridge.Id, stasisStartEvent.ChannelId);

        Channel targetChannel = await this.asteriskBroker.InsertChannelAsync($"PJSIP/{targetExtension}");
        this.pendingBridgeIdByChannelId[targetChannel.ChannelId] = bridge.Id;
    }

    private async Task HandleChannelStateChangeAsync(ChannelStateChangeEvent stateChangeEvent)
    {
        if (stateChangeEvent.State != UpChannelState)
        {
            return;
        }

        if (this.pendingBridgeIdByChannelId.TryRemove(stateChangeEvent.ChannelId, out string? bridgeId))
        {
            await this.asteriskBroker.AddChannelToBridgeAsync(bridgeId, stateChangeEvent.ChannelId);
        }
    }
}
