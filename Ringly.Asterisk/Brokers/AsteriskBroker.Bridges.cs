using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string BridgesRelativeUrl = "bridges";

    public async ValueTask<Bridge> InsertBridgeAsync(string bridgeType) =>
        await this.PostAsync<Bridge>($"{BridgesRelativeUrl}?type={Uri.EscapeDataString(bridgeType)}");

    public async ValueTask AddChannelToBridgeAsync(string bridgeId, string channelId) =>
        await this.PostAsync(
            $"{BridgesRelativeUrl}/{Uri.EscapeDataString(bridgeId)}/addChannel" +
            $"?channel={Uri.EscapeDataString(channelId)}");
}
