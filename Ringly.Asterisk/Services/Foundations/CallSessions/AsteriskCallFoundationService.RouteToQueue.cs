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

        // No "PJSIP/" prefix — matches StartCallSessionAsync's existing, acceptance-tested
        // convention of passing the bare extension straight through to ARI's originate endpoint.
        Channel customerChannel = await this.asteriskBroker.InsertChannelAsync(credentials.Extension);

        await this.asteriskBroker.AddChannelToBridgeAsync(holdingBridge.BridgeId, customerChannel.ChannelId);

        return new CallSession
        {
            CallSessionId = Guid.NewGuid(),
            BridgeId = holdingBridge.BridgeId,
            CustomerChannelId = customerChannel.ChannelId
        };
    });
}
