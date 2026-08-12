using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Brokers;

public partial interface ISipTrunkBroker
{
    ValueTask ConfigureTrunkAsync(SipTrunkConfig config);
    ValueTask RemoveTrunkAsync(string trunkName);
}
