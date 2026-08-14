using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
{
    // Cold support entry point (§3.1a) — customer taps "contact support" with no active call in
    // progress. NOT for escalating an already-connected call.
    public ValueTask<CallSession> RouteToQueueAsync(Guid customerId, string queueName) =>
    TryCatch(async () =>
    {
        ValidateRouteToQueueRequest(customerId, queueName);

        SipCredentials? credentials = await this.sipCredentialsStore.RetrieveByClientIdAsync(customerId);

        if (credentials is null)
        {
            throw new NotFoundSipCredentialsException(customerId);
        }

        HoldingBridge? holdingBridge = await this.queueRegistry.RetrieveByNameAsync(queueName);

        if (holdingBridge is null)
        {
            throw new NotFoundQueueException(queueName);
        }

        // "PJSIP/" prefix required — ARI's /channels?endpoint= needs a "Tech/Resource" string;
        // a bare extension is rejected outright with 400 "Invalid endpoint specified" (confirmed
        // against a real Asterisk instance). The prior comment here claiming this was
        // "acceptance-tested" without the prefix was wrong — no such test exists in this repo.
        Channel customerChannel = await this.asteriskBroker.InsertChannelAsync($"PJSIP/{credentials.Extension}");

        // See StartCallSessionAsync's own comment — a just-originated channel isn't biddable
        // until it actually enters the Stasis app.
        await WaitForStasisStartAsync(this.asteriskBroker, customerChannel.ChannelId);

        await this.asteriskBroker.AddChannelToBridgeAsync(holdingBridge.BridgeId, customerChannel.ChannelId);

        return new CallSession
        {
            CallSessionId = Guid.NewGuid(),
            BridgeId = holdingBridge.BridgeId,
            CustomerChannelId = customerChannel.ChannelId
        };
    });
}
