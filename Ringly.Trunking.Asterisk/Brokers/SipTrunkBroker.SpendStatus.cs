using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models;

namespace Ringly.Trunking.Asterisk.Brokers;

public partial class SipTrunkBroker
{
    private const string TrunkChannelNamePrefixFormat = "PJSIP/{0}-";

    // ActiveCallCount is real — derived from Asterisk's own channel list, filtered by the
    // PJSIP channel naming convention "PJSIP/{endpoint}-{uniqueid}". SpendTodayUsd is NOT
    // populated here: Asterisk has no native visibility into per-call cost, that requires the
    // trunk provider's own billing/usage API (out of scope for this row, tracked separately —
    // see §8.4/§8.7 item 13). IsOverLimit is left false; comparing against SipTrunkConfig's
    // limits is the Foundation Service's job (row #25), not the broker's.
    public async ValueTask<TrunkCallLimitStatus> RetrieveSpendStatusAsync(string trunkName)
    {
        List<AriChannelListItem> channels = await this.GetAsync<List<AriChannelListItem>>(ChannelsRelativeUrl);
        string namePrefix = string.Format(TrunkChannelNamePrefixFormat, trunkName);

        int activeCallCount = channels.Count(channel => channel.Name.StartsWith(namePrefix, StringComparison.Ordinal));

        return new TrunkCallLimitStatus
        {
            TrunkName = trunkName,
            ActiveCallCount = activeCallCount,
            SpendTodayUsd = 0m,
            IsOverLimit = false
        };
    }
}
