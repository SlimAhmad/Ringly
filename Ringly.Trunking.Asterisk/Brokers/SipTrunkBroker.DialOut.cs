using Ringly.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models;

namespace Ringly.Trunking.Asterisk.Brokers;

public partial class SipTrunkBroker
{
    private const string ChannelsRelativeUrl = "channels";

    // Dials PJSIP/{phoneNumber}@{trunkName} — {trunkName} is the endpoint ConfigureTrunkAsync
    // configured, acting as the outbound target/proxy. Matches Ringly.Asterisk.AsteriskBroker
    // .InsertChannelAsync's verified endpoint+app shape (row #3/PR #38).
    public async ValueTask<Channel> DialOutAsync(string phoneNumber, string trunkName)
    {
        string endpoint = $"PJSIP/{phoneNumber}@{trunkName}";

        AriChannelResponse response = await this.PostAsync<AriChannelResponse>(
            $"{ChannelsRelativeUrl}?endpoint={Uri.EscapeDataString(endpoint)}" +
            $"&app={Uri.EscapeDataString(this.trunkOptions.StasisAppName)}");

        return new Channel { ChannelId = response.Id };
    }
}
