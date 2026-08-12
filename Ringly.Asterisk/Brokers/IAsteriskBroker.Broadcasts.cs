using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    IObservable<CallBroadcastEvent> StreamCallBroadcasts();
    ValueTask<ClaimResult> ClaimCallAsync(string channelId, string agentAppName);
    ValueTask SetAgentAvailabilityAsync(string agentAppName, bool isAvailable);
}
