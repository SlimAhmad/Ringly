using System.Reactive.Linq;
using Microsoft.Extensions.Options;
using Ringly.Asterisk.Brokers;
using Ringly.Client.Abstractions;
using Ringly.Client.Abstractions.Models;
using Ringly.Client.SipSorcery;

namespace Ringly.Samples.WebApi;

// Row #38f — the real, always-registered SIP endpoint Dograh's native Call Transfer tool needs
// (see SupportController.PostDograhTransferResolverAsync's own comment for why: Dograh's tool
// can only target a genuine PJSIP/SIP endpoint with a real registered contact, and chan_pjsip
// itself fundamentally requires one to dial - there's no way to make an ARI-originated PJSIP/xxx
// channel land directly in dialplan without one).
//
// Confirmed live, twice, as a hard architectural constraint (not a bug to patch around): Dograh's
// own ARI app keeps Stasis/bridge ownership of the caller's channel for the life of the call, even
// after its own "Call Transfer" tool bridges the caller with this endpoint - the instant anything
// (a raw ARI /move, or this service's own earlier attempt at a SIP BlindTransfer/REFER) tries to
// move or remove a channel FROM that bridge, Dograh's own app concludes the call ended and tears
// its whole side down within milliseconds, taking the caller's channel down with it.
//
// The fix, confirmed against Dograh's own explanation of their architecture: never move or remove
// anything from the bridge Dograh created. Instead, once this service's own client answers and
// Dograh's transfer tool finishes its own bridge swap, ADD the claiming agent's channel directly
// into that same bridge (ConnectAgentToBridgeAsync) - a bridge addition doesn't take ownership
// away from Dograh's app the way a removal does. This service's own job is just to: answer, find
// the bridge Dograh put it in, start MOH on it (standing in for Asterisk's own automatic
// holding-bridge MOH, which doesn't apply here since this isn't a holding bridge), and publish the
// same waiting-customer broadcast every other queue entry uses - AgentsController.PostClaimAsync
// does the actual bridge-vs-holding-bridge branching from there.
public class QueueTransferRegistrarService : BackgroundService
{
    private const string RegistrarExtension = "supportregistrar";
    private const string RegistrarPassword = "ringly-dev-supportregistrar";
    private const string RegistrarChannelNamePrefix = "PJSIP/supportregistrar-";
    private const string TargetQueueName = "support";
    private const string MohClass = "default";
    private const int BridgeDiscoveryAttempts = 20;
    private const int BridgeDiscoveryDelayMilliseconds = 500;

    private readonly ICallClient callClient;
    private readonly IAsteriskBroker asteriskBroker;
    private readonly SupportQueueBroadcastRegistry supportQueueBroadcastRegistry;
    private readonly ILogger<QueueTransferRegistrarService> logger;

    public QueueTransferRegistrarService(
        IOptions<SipSorceryCallOptions> options,
        IAsteriskBroker asteriskBroker,
        SupportQueueBroadcastRegistry supportQueueBroadcastRegistry,
        ILogger<QueueTransferRegistrarService> logger)
    {
        this.callClient = new SipSorceryCallClient(options);
        this.asteriskBroker = asteriskBroker;
        this.supportQueueBroadcastRegistry = supportQueueBroadcastRegistry;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await this.callClient.RegisterAsync(new SipCredentials
        {
            ClientId = Guid.NewGuid(),
            Extension = RegistrarExtension,
            Password = RegistrarPassword
        });

        IDisposable subscription = this.callClient.StreamEvents()
            .Where(callClientEvent => callClientEvent.EventType == "IncomingCall")
            .Subscribe(callClientEvent => this.OnIncomingCall(callClientEvent.Handle));

        stoppingToken.Register(() => subscription.Dispose());

        // Keeps this BackgroundService's own execution alive for the app's lifetime - all the
        // real work happens in the event subscription above, same idiom as
        // RideHailingCallRouter's own ExecuteAsync.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async void OnIncomingCall(CallHandle handle)
    {
        try
        {
            await this.callClient.AnswerCallAsync(handle);

            string? bridgeId = await this.DiscoverBridgeIdAsync();

            if (bridgeId is null)
            {
                this.logger.LogError(
                    "Could not find the bridge Dograh's transfer put call {CallHandleId} in " +
                    "after {Attempts} attempts - hanging up.",
                    handle.Id,
                    BridgeDiscoveryAttempts);

                await this.callClient.HangupAsync(handle);
                return;
            }

            // Standing in for Asterisk's own automatic holding-bridge MOH (row #7's own
            // StartMusicOnHoldAsync comment) - this bridge is Dograh's, not a "holding" bridge
            // Ringly created, so nothing plays automatically without this.
            await this.asteriskBroker.StartMusicOnHoldAsync(bridgeId, MohClass);

            // isExternalBridge: true - see SupportQueueBroadcastRegistry's own comment on
            // WaitingEntry for why this matters at claim time.
            this.supportQueueBroadcastRegistry.PublishWaitingCustomer(
                clientId: Guid.NewGuid(),
                queueName: TargetQueueName,
                channelId: handle.Id,
                bridgeId: bridgeId,
                isExternalBridge: true);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to answer and queue an incoming call ({CallHandleId}) via Dograh's transfer.",
                handle.Id);
        }
    }

    // No "bridge swap finished" event to wait on directly (Dograh's own bridge-swap step runs on
    // its own timeline, confirmed live to take several seconds after this client's own answer) -
    // polling ARI's own channel/bridge listing is the only way to discover it from this side.
    private async Task<string?> DiscoverBridgeIdAsync()
    {
        for (int attempt = 0; attempt < BridgeDiscoveryAttempts; attempt++)
        {
            string? bridgeId =
                await this.asteriskBroker.RetrieveBridgeIdByChannelNamePrefixAsync(RegistrarChannelNamePrefix);

            if (bridgeId is not null)
            {
                return bridgeId;
            }

            await Task.Delay(BridgeDiscoveryDelayMilliseconds);
        }

        return null;
    }
}
