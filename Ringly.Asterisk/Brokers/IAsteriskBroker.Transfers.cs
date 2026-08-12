using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial interface IAsteriskBroker
{
    IObservable<ChannelTransferEvent> StreamTransferRequests();
    ValueTask SendTransferProgressAsync(string channelId, TransferState state);
}
