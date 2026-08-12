using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    ValueTask<IReadOnlyList<ConfigTuple>> RetrieveSipEndpointConfigAsync(string extension);
    ValueTask InsertSipEndpointConfigAsync(SipEndpointConfig config);
}
