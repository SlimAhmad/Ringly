using Ringly.Abstractions.Models;

namespace Ringly.CallCenter.Abstractions;

public partial interface ICallCenterProvider
{
    IObservable<ChannelTransferEvent> StreamTransferRequests();
    ValueTask SendTransferProgressAsync(string channelId, TransferState state);
}
