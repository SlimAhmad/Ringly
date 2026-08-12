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
}
