using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    IObservable<StasisStartEvent> StreamStasisStartEvents();
    IObservable<StasisEndEvent> StreamStasisEndEvents();
    IObservable<ChannelStateChangeEvent> StreamChannelStateChangeEvents();
    ValueTask AnswerChannelAsync(string channelId);
    ValueTask RingChannelAsync(string channelId);
}
