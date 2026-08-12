using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    ValueTask<Bridge> InsertBridgeAsync(string bridgeType);
}
