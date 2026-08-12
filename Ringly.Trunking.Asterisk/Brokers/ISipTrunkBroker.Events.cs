using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Brokers;

public partial interface ISipTrunkBroker
{
    IObservable<TrunkCallEvent> StreamInboundTrunkCallsAsync();
}
