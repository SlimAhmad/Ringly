using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Brokers;

public partial interface ISipTrunkBroker
{
    ValueTask<TrunkCallLimitStatus> RetrieveSpendStatusAsync(string trunkName);
}
