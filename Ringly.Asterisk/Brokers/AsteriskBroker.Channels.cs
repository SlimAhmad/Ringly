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

    // ARI's POST /channels/{id}/move reassigns an already-Stasis'd channel to a different app —
    // confirmed against a real Asterisk instance, requires only that the channel is currently in
    // *some* Stasis app, not specifically this one. Lets Ringly's own app take control of a
    // channel an external party (e.g. Dograh, on its own independent Stasis app) is holding.
    public async ValueTask MoveChannelAsync(string channelId) =>
        await this.PostAsync(
            $"{ChannelsRelativeUrl}/{Uri.EscapeDataString(channelId)}/move" +
            $"?app={Uri.EscapeDataString(this.asteriskOptions.StasisAppName)}");

    // ARI's GET /channels lists every live channel on the whole Asterisk instance (not scoped to
    // any one Stasis app) — the only way to resolve a channel id for an escalating external AI
    // agent (e.g. Dograh) that can only ever supply the caller's own phone number to a tool call,
    // never the channel id itself. Picks the first match; a genuine same-number concurrent-call
    // collision is out of scope here (mirrors how RouteToQueueAsync itself has no more specific
    // signal to disambiguate on either).
    public async ValueTask<string?> RetrieveChannelIdByCallerNumberAsync(string callerNumber)
    {
        List<AriChannelResponse> channels = await this.GetAsync<List<AriChannelResponse>>(ChannelsRelativeUrl);

        return channels.FirstOrDefault(channel => channel.Caller.Number == callerNumber)?.Id;
    }
}
