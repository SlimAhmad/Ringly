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

    // Row #38f — the only way to find a bridge an external ARI app (e.g. Dograh) created and
    // still owns: there's no "get bridge by channel" ARI resource, so this lists every live
    // channel to resolve the name (e.g. "PJSIP/supportregistrar-...") to an id, then lists every
    // live bridge to find which one currently contains that channel id. Read-only (GET both
    // sides) - deliberately doesn't touch anything Dograh's own app is tracking, unlike a
    // move/transfer would.
    public async ValueTask<string?> RetrieveBridgeIdByChannelNamePrefixAsync(string channelNamePrefix)
    {
        List<AriChannelResponse> channels = await this.GetAsync<List<AriChannelResponse>>(ChannelsRelativeUrl);

        string? channelId = channels
            .FirstOrDefault(channel => channel.Name.StartsWith(channelNamePrefix, StringComparison.Ordinal))
            ?.Id;

        if (channelId is null)
        {
            return null;
        }

        List<Bridge> bridges = await this.GetAsync<List<Bridge>>(BridgesRelativeUrl);

        return bridges.FirstOrDefault(bridge => bridge.Channels.Contains(channelId))?.Id;
    }
}
