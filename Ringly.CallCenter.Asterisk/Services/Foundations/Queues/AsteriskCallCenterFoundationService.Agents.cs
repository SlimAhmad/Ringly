using Ringly.Abstractions.Models;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService
{
    public IObservable<CallBroadcastEvent> StreamCallBroadcasts() =>
        this.asteriskBroker.StreamCallBroadcasts();

    public ValueTask<ClaimResult> ClaimCallAsync(string channelId, string agentAppName) =>
    TryCatchClaim(async () =>
    {
        ValidateAgentRequest(channelId, agentAppName);

        return await this.asteriskBroker.ClaimCallAsync(channelId, agentAppName);
    });

    public ValueTask SetAgentAvailabilityAsync(string agentAppName, bool isAvailable) =>
    TryCatchAgent(async () =>
    {
        ValidateAgentAppName(agentAppName);

        await this.asteriskBroker.SetAgentAvailabilityAsync(agentAppName, isAvailable);
    });
}
