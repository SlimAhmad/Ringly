using System.Reactive.Subjects;
using Ringly.Abstractions.Models;

namespace Ringly.Twilio.Events;

public class TwilioCallEventStream : ITwilioCallEventStream, IDisposable
{
    private readonly Subject<CallEvent> callEvents = new();

    public IObservable<CallEvent> Events => this.callEvents;

    public void Publish(CallEvent callEvent) =>
        this.callEvents.OnNext(callEvent);

    public void Dispose() =>
        this.callEvents.Dispose();
}
