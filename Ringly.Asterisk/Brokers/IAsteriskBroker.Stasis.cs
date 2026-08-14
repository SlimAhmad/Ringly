using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    IObservable<StasisStartEvent> StreamStasisStartEvents();
    ValueTask AnswerChannelAsync(string channelId);
}
