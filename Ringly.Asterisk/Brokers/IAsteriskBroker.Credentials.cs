using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    ValueTask InsertSipEndpointConfigAsync(SipEndpointConfig config);
}
