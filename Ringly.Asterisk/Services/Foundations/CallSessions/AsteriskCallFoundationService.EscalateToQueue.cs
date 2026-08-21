using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
{
    // Escalation path for a call Ringly's own Stasis app never originated (e.g. one an external
    // AI agent like Dograh is currently handling on its own, independent Stasis app) — ARI's
    // /channels/{id}/move reassigns an already-Stasis'd channel to a different app (confirmed
    // against a real Asterisk instance; requires only that the channel is currently in *some*
    // Stasis app, not specifically this one), which is what actually lets Ringly's own app take
    // control and bridge the channel into a queue.
    public ValueTask<CallSession> EscalateToQueueAsync(string channelId, string queueName) =>
    TryCatch(async () =>
    {
        ValidateEscalateToQueueRequest(channelId, queueName);

        HoldingBridge? holdingBridge = await this.queueRegistry.RetrieveByNameAsync(queueName);

        if (holdingBridge is null)
        {
            throw new NotFoundQueueException(queueName);
        }

        await this.asteriskBroker.MoveChannelAsync(channelId);

        // Moving a channel to a new Stasis app raises a fresh StasisStart there — same "not
        // biddable until it's actually in the app" wait as a freshly-originated channel (see
        // StartCallSessionAsync's own comment).
        await WaitForStasisStartAsync(this.asteriskBroker, channelId);

        await this.asteriskBroker.AddChannelToBridgeAsync(holdingBridge.BridgeId, channelId);

        return new CallSession
        {
            CallSessionId = Guid.NewGuid(),
            BridgeId = holdingBridge.BridgeId,
            CustomerChannelId = channelId
        };
    });
}
