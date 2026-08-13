using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Brokers;

public partial interface ISipTrunkBroker
{
    ValueTask ConfigureTrunkAsync(SipTrunkConfig config);
    ValueTask<SipTrunkConfig> RetrieveTrunkConfigAsync(string trunkName);
    ValueTask<IReadOnlyList<string>> ListConfiguredTrunkNamesAsync();
    ValueTask RemoveTrunkAsync(string trunkName);
}
