using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    ValueTask<Bridge> InsertBridgeAsync(string bridgeType);
    ValueTask AddChannelToBridgeAsync(string bridgeId, string channelId);
    ValueTask RemoveChannelFromBridgeAsync(string bridgeId, string channelId);
    ValueTask StartMusicOnHoldAsync(string bridgeId, string mohClass);
    ValueTask StopMusicOnHoldAsync(string bridgeId);
}
