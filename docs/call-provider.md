# Call Provider

`Ringly.Abstractions` defines the core calling contracts every backend implements.
Your app depends on these interfaces, not on Asterisk or Twilio directly.

```csharp
public partial interface ICallProvider
{
    ValueTask<CallSession> StartCallSessionAsync(CallParticipant partyA, CallParticipant partyB);

    // Cold support entry point — customer taps "contact support" with no active call
    // in progress. NOT for escalating an already-connected call.
    ValueTask<CallSession> RouteToQueueAsync(Guid customerId, string queueName);
}

public partial interface ICallProvisioningService
{
    ValueTask<SipCredentials> AddClientCredentialsAsync(Guid clientId);
}

// App-owned — no implementation ships from Ringly. See docs/getting-started.md for
// a minimal in-memory example.
public interface ISipCredentialsStore
{
    ValueTask<SipCredentials?> RetrieveByClientIdAsync(Guid clientId);
}
```

Key models: `CallParticipant { Guid Id; string SipExtension }`,
`CallSession { Guid CallSessionId; string BridgeId; Guid TripId; string CustomerChannelId }`,
`SipCredentials { Guid ClientId; string Extension; string Password }`.

`RouteToQueueAsync` needs `IQueueRegistry` too (from `Ringly.CallCenter.Abstractions`)
— see [call-center.md](call-center.md).

## Asterisk — `Ringly.Asterisk`

```csharp
dotnet add package Ringly.Asterisk
```

```csharp
services.Configure<AsteriskOptions>(options =>
{
    options.BaseUrl = "http://localhost:8088";
    options.Username = "ringly";
    options.Password = "ringly-dev-ari";
    options.StasisAppName = "ride_hailing_app";
    options.DialplanContext = "ride_hailing";
    options.UseWebRtcTransport = true;   // set false for plain SIP/UDP clients
    options.AmiPort = 5038;
    options.AmiUsername = "ringly";
    options.AmiSecret = "ringly-dev-ami";
});

services.AddSingleton<ILoggingBroker, ConsoleLoggingBroker>(); // Ringly.Asterisk.Brokers.ILoggingBroker

// Owns a persistent ARI WebSocket + AMI TCP connection — register as a singleton.
services.AddSingleton<IAsteriskBroker, AsteriskBroker>();

services.AddSingleton<ISipCredentialsStore, /* your implementation */>();

services.AddScoped<IAsteriskSipEndpointConfigFoundationService, AsteriskSipEndpointConfigFoundationService>();
services.AddScoped<ICallProvisioningService, CallProvisioningService>();
services.AddScoped<ICallProvider, AsteriskCallFoundationService>();
```

`AsteriskCallFoundationService` (implements `ICallProvider`) constructor:

```csharp
public AsteriskCallFoundationService(
    IAsteriskBroker asteriskBroker,
    ISipCredentialsStore sipCredentialsStore,
    IQueueRegistry queueRegistry,     // from Ringly.CallCenter.Abstractions
    ILoggingBroker loggingBroker)
```

`CallProvisioningService` (implements `ICallProvisioningService`) generates a random
6-digit extension + password and provisions it against Asterisk's dynamic PJSIP
config (needs the realtime Postgres backend — see
[docker/README.md](../docker/README.md)):

```csharp
public CallProvisioningService(
    IAsteriskSipEndpointConfigFoundationService sipEndpointConfigFoundationService,
    ILoggingBroker loggingBroker)
```

Usage:

```csharp
CallSession session = await callProvider.StartCallSessionAsync(
    new CallParticipant { SipExtension = "1000" },
    new CallParticipant { SipExtension = "1001" });

SipCredentials credentials = await provisioningService.AddClientCredentialsAsync(clientId);
```

Full walkthrough (including `RouteToQueueAsync`): [getting-started.md](getting-started.md).

## Twilio — `Ringly.Twilio`

```csharp
dotnet add package Ringly.Twilio
```

```csharp
services.Configure<TwilioOptions>(options =>
{
    options.AccountSid = "AC...";
    options.AuthToken = "...";
    options.DefaultCallerId = "+15551234567"; // a number your Twilio account owns/verifies
});

services.AddSingleton<Ringly.Twilio.Brokers.ILoggingBroker, ConsoleLoggingBroker>();
services.AddSingleton<ITwilioBroker, TwilioBroker>();
services.AddSingleton<ISipCredentialsStore, /* your implementation */>();
services.AddScoped<ICallProvider, TwilioCallProvider>();
```

`TwilioCallProvider` constructor:

```csharp
public TwilioCallProvider(
    ITwilioBroker twilioBroker,
    ISipCredentialsStore sipCredentialsStore,
    ILoggingBroker loggingBroker,     // Ringly.Twilio.Brokers.ILoggingBroker — a separate
                                       // type from Ringly.Asterisk.Brokers.ILoggingBroker,
                                       // register/implement both if you use both backends
    IOptions<TwilioOptions> twilioOptions)
```

`StartCallSessionAsync` dials both parties into a freshly-named Twilio conference
(auto-created on first dial-in, no separate "create conference" step).
`RouteToQueueAsync` dials the customer straight into a conference named after the
queue — Twilio's implementation needs no `IQueueRegistry` at all, unlike Asterisk's
(see [call-center.md](call-center.md) for why).

### Inbound webhooks

Twilio delivers call status events as inbound HTTP webhooks, not a persistent
connection. `TwilioWebhookController` receives them:

```csharp
// Program.cs — Ringly.Twilio needs FrameworkReference Microsoft.AspNetCore.App,
// already set on the package; your host just needs to discover the controller.
builder.Services.AddControllers();
builder.Services.Configure<TwilioWebhookOptions>(options =>
{
    options.AuthToken = "..."; // same Auth Token as TwilioOptions
});
builder.Services.AddSingleton<ITwilioSignatureValidator, TwilioSignatureValidator>();
builder.Services.AddSingleton<ITwilioCallEventStream, TwilioCallEventStream>(); // must be singleton

var app = builder.Build();
app.MapControllers();
```

Exposes `POST /webhooks/twilio/voice` — point your Twilio phone number's voice
webhook (or a TwiML `<Connect action="...">`) at
`https://your-host/webhooks/twilio/voice`. Every request's `X-Twilio-Signature` is
verified before anything is trusted; invalid signatures get a `403`. Valid events
are published to `ITwilioCallEventStream.Events` (`IObservable<CallEvent>`) — your
app subscribes to react to them.
