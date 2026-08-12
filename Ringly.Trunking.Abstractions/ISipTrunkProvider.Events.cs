using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Abstractions;

public partial interface ISipTrunkProvider
{
    IObservable<TrunkCallEvent> StreamInboundTrunkCallsAsync();
}
