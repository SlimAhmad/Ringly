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

    public async ValueTask RemoveChannelFromBridgeAsync(string bridgeId, string channelId) =>
        await this.PostAsync(
            $"{BridgesRelativeUrl}/{Uri.EscapeDataString(bridgeId)}/removeChannel" +
            $"?channel={Uri.EscapeDataString(channelId)}");

    // Only meaningful on a "holding" bridge — starting MOH on it plays to every current AND
    // future member automatically (Asterisk's own documented holding-bridge behavior), so this is
    // called once at queue-creation time, not per-customer.
    public async ValueTask StartMusicOnHoldAsync(string bridgeId, string mohClass) =>
        await this.PostAsync(
            $"{BridgesRelativeUrl}/{Uri.EscapeDataString(bridgeId)}/moh" +
            $"?mohClass={Uri.EscapeDataString(mohClass)}");

    public async ValueTask StopMusicOnHoldAsync(string bridgeId) =>
        await this.DeleteAsync($"{BridgesRelativeUrl}/{Uri.EscapeDataString(bridgeId)}/moh");
}
