using Ringly.Abstractions.Models;

namespace Ringly.Twilio.Events;

// Twilio delivers events as inbound webhooks, not an outbound WebSocket like Asterisk's ARI —
// TwilioWebhookController is the producer, this is the shared sink every subscriber reads
// from. Must be registered as a singleton (the controller is per-request/transient by default,
// but the underlying Subject needs to outlive any single request).
public interface ITwilioCallEventStream
{
    IObservable<CallEvent> Events { get; }
    void Publish(CallEvent callEvent);
}
