using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string ChannelsRelativeUrl = "channels";

    public async ValueTask<Channel> InsertChannelAsync(string endpoint)
    {
        AriChannelResponse response = await this.PostAsync<AriChannelResponse>(
            $"{ChannelsRelativeUrl}?endpoint={Uri.EscapeDataString(endpoint)}" +
            $"&app={Uri.EscapeDataString(this.asteriskOptions.StasisAppName)}");

        return new Channel { ChannelId = response.Id };
    }

    // ARI's DELETE /channels/{id} hangs the channel up. Used by RideHailingCallRouter to clean up
    // the peer leg of a call when the other leg disappears from the Stasis app (crash, force-close,
    // or a normal hangup) — without this, a channel whose peer vanished mid-ring or mid-call has no
    // way to ever be torn down, since nothing else in this flow ever calls Dial() or otherwise puts
    // Asterisk's own channel-supervision in charge of it.
    public async ValueTask HangupChannelAsync(string channelId) =>
        await this.DeleteAsync($"{ChannelsRelativeUrl}/{Uri.EscapeDataString(channelId)}");
}
