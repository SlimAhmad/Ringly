using Ringly.Abstractions.Models;

namespace Ringly.CallCenter.Abstractions;

public partial interface ICallCenterProvider
{
    IObservable<CallBroadcastEvent> StreamCallBroadcasts();
    ValueTask<ClaimResult> ClaimCallAsync(string channelId, string agentAppName);
    ValueTask SetAgentAvailabilityAsync(string agentAppName, bool isAvailable);
}
