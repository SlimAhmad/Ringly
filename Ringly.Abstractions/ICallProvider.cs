using Ringly.Abstractions.Models;

namespace Ringly.Abstractions;

public interface ICallProvider
{
    ValueTask<CallSession> StartCallSessionAsync(CallParticipant partyA, CallParticipant partyB);
    ValueTask EndCallSessionAsync(Guid callSessionId);
    ValueTask<Channel> OriginateAsync(string endpoint);
    ValueTask<CallSession> RouteToQueueAsync(Guid customerId, string queueName);
    IObservable<CallEvent> StreamCallEvents();
    IObservable<DtmfEvent> StreamDtmfEvents();
}
