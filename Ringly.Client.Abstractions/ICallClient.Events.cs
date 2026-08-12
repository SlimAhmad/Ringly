using Ringly.Client.Abstractions.Models;

namespace Ringly.Client.Abstractions;

public partial interface ICallClient
{
    IObservable<CallClientEvent> StreamEvents();
}
