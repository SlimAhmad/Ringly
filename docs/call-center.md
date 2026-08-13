# Call Center

`Ringly.CallCenter.Abstractions` defines queue and transfer contracts:

```csharp
public partial interface ICallCenterProvider
{
    ValueTask<HoldingBridge> CreateQueueAsync(QueueConfig config);

    // Reactive-only — no request/response transfer methods. A channel-transfer request
    // (e.g. a SIP REFER) arrives as an event; you respond by publishing progress states.
    IObservable<ChannelTransferEvent> StreamTransferRequests();
    ValueTask SendTransferProgressAsync(string channelId, TransferState state);
}

// App-owned — no implementation ships from Ringly.
public interface IQueueRegistry
{
    ValueTask<HoldingBridge?> RetrieveByNameAsync(string queueName);
    ValueTask RegisterAsync(HoldingBridge holdingBridge); // called by CreateQueueAsync on creation
}
```

Models: `QueueConfig { string Name; string MusicOnHoldClass }`,
`HoldingBridge { string BridgeId; string QueueName }`.

## Asterisk — `Ringly.CallCenter.Asterisk`

A queue is an Asterisk "holding" bridge; `CreateQueueAsync` also registers it into
`IQueueRegistry` so `ICallProvider.RouteToQueueAsync` (see
[call-provider.md](call-provider.md)) can find it by name later.

```csharp
dotnet add package Ringly.CallCenter.Asterisk
```

```csharp
services.AddSingleton<IQueueRegistry, /* your implementation */>();
services.AddScoped<ICallCenterProvider, AsteriskCallCenterFoundationService>();
```

```csharp
public AsteriskCallCenterFoundationService(
    IAsteriskBroker asteriskBroker,     // Ringly.Asterisk — reuse the same singleton
    IQueueRegistry queueRegistry,
    ILoggingBroker loggingBroker)       // Ringly.Asterisk.Brokers.ILoggingBroker
```

```csharp
HoldingBridge queue = await callCenterProvider.CreateQueueAsync(
    new QueueConfig { Name = "support" });

callCenterProvider.StreamTransferRequests().Subscribe(async transferEvent =>
{
    // React to a phone-initiated transfer (SIP REFER surfaced via ARI) and report progress.
    await callCenterProvider.SendTransferProgressAsync(
        transferEvent.ChannelId, TransferState.ChannelAnswered);
});
```

## Twilio — `Ringly.CallCenter.Twilio`

A queue maps to a Twilio TaskRouter TaskQueue.

```csharp
dotnet add package Ringly.CallCenter.Twilio
```

```csharp
services.AddScoped<ICallCenterProvider, TwilioCallCenterProvider>();
```

```csharp
public TwilioCallCenterProvider(
    ITwilioBroker twilioBroker,       // Ringly.Twilio — reuse the same singleton
    ILoggingBroker loggingBroker)     // Ringly.Twilio.Brokers.ILoggingBroker
```

`CreateQueueAsync` needs `TwilioOptions.WorkspaceSid` set (see
[call-provider.md](call-provider.md)'s Twilio section) — TaskQueues live under a
TaskRouter Workspace. No `IQueueRegistry` is needed for this backend at all: Twilio
auto-creates conferences by friendly name, so `ICallProvider.RouteToQueueAsync`'s
Twilio implementation just dials into a conference named after the queue directly.

**Transfers are not supported on Twilio.** `StreamTransferRequests()` never emits
and `SendTransferProgressAsync` only validates and no-ops — this is a real,
confirmed platform gap, not a bug: Twilio's Voice/TaskRouter APIs have no
equivalent to Asterisk ARI's SIP-REFER-detection event, on any integration path.
The methods exist only so `TwilioCallCenterProvider` satisfies `ICallCenterProvider`.
