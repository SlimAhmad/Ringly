using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    IObservable<StasisStartEvent> StreamStasisStartEvents();
    IObservable<ChannelStateChangeEvent> StreamChannelStateChangeEvents();
    ValueTask AnswerChannelAsync(string channelId);
}
