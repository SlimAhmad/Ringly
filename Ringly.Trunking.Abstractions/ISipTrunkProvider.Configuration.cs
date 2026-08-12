using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Abstractions;

public partial interface ISipTrunkProvider
{
    ValueTask ConfigureTrunkAsync(SipTrunkConfig config);
    ValueTask RemoveTrunkAsync(string trunkName);
}
