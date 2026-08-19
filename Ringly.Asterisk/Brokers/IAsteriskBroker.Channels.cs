using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    ValueTask<Channel> InsertChannelAsync(string endpoint);
    ValueTask HangupChannelAsync(string channelId);
}
