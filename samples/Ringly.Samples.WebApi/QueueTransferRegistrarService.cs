using System.Reactive.Linq;
using Microsoft.Extensions.Options;
using Ringly.Client.Abstractions;
using Ringly.Client.Abstractions.Models;
using Ringly.Client.SipSorcery;

namespace Ringly.Samples.WebApi;

// Row #38e — the real, always-registered SIP endpoint Dograh's native Call Transfer tool
// actually needs. Confirmed live that Dograh's own tool can only target a genuine "PJSIP/SIP"
// endpoint with a real registered contact - a Local channel destination gets its tech/resource
// mis-parsed by Dograh's own code ("Unable to create PJSIP channel - endpoint 'Local' was not
// found"), and chan_pjsip itself fundamentally requires a real network peer to dial (there's no
// way to make an ARI-originated PJSIP/xxx channel land directly in dialplan without one).
// Confirmed live (separately) that once Dograh's transfer connects, Dograh's own app directly
// bridges the caller to whatever answered - bypassing Ringly's own Stasis app/holding
// bridge/MOH/claim system entirely - so simply auto-answering isn't enough on its own. This
// service answers, then immediately sends a real SIP BlindTransfer (REFER) back into Asterisk
// targeting the actual queue name (e.g. "support"), which extensions.conf's own _[a-z]. pattern
// hands to Stasis(ride_hailing_app,...), where RideHailingCallRouter's own queue-name check
// bridges it into that queue's real holding bridge - the same path a native Ringly customer or a
// Dograh caller through the raw ARI /move approach would take.
public class QueueTransferRegistrarService : BackgroundService
{
    private const string RegistrarExtension = "supportregistrar";
    private const string RegistrarPassword = "ringly-dev-supportregistrar";
    private const string TargetQueueExtension = "support";

    private readonly ICallClient callClient;
    private readonly ILogger<QueueTransferRegistrarService> logger;

    public QueueTransferRegistrarService(
        IOptions<SipSorceryCallOptions> options, ILogger<QueueTransferRegistrarService> logger)
    {
        this.callClient = new SipSorceryCallClient(options);
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

            bool transferred = await this.callClient.BlindTransferAsync(handle, TargetQueueExtension);

            if (!transferred)
            {
                this.logger.LogError(
                    "Blind transfer to queue '{QueueExtension}' was not accepted for call {CallHandleId}.",
                    TargetQueueExtension,
                    handle.Id);

                await this.callClient.HangupAsync(handle);
            }
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to answer and transfer an incoming call ({CallHandleId}) into the queue.",
                handle.Id);
        }
    }
}
