# Getting Started

This walks through wiring up Ringly's Asterisk backend end-to-end: provisioning a
client, creating a call center queue, starting a call, and routing a cold support
request into a queue. It's the fastest way to see how the pieces fit together — for
Twilio or other capabilities, see the docs linked from the [README](../README.md).

## 1. Reference the packages

```bash
dotnet add package Ringly.Abstractions
dotnet add package Ringly.Asterisk
dotnet add package Ringly.CallCenter.Abstractions
dotnet add package Ringly.CallCenter.Asterisk
```

## 2. Stand up a local Asterisk instance (optional, for real testing)

```bash
cd docker
docker compose up -d
```

This gives you Asterisk 23.4.1 on `localhost:8088` (ARI), with a seeded test SIP
endpoint (extension `1000` / password `ringly-dev-1000`). See
[docker/README.md](../docker/README.md) for full details. Everything below also
compiles and runs against a real Asterisk instance you manage yourself — just point
`AsteriskOptions` at it.

## 3. Implement the two store contracts Ringly needs from your app

`ISipCredentialsStore` and `IQueueRegistry` are persistence contracts Ringly depends
on but doesn't own storage for — your app decides where credentials and queue
registrations live (a database, in this example just memory). No implementation
ships from the library on purpose.

```csharp
using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;
using System.Collections.Concurrent;

public class InMemorySipCredentialsStore : ISipCredentialsStore
{
    private readonly ConcurrentDictionary<Guid, SipCredentials> credentials = new();

    public void Add(SipCredentials sipCredentials) =>
        this.credentials[sipCredentials.ClientId] = sipCredentials;

    public ValueTask<SipCredentials?> RetrieveByClientIdAsync(Guid clientId) =>
        ValueTask.FromResult(this.credentials.GetValueOrDefault(clientId));
}

public class InMemoryQueueRegistry : IQueueRegistry
{
    private readonly ConcurrentDictionary<string, HoldingBridge> queues = new();

    public ValueTask<HoldingBridge?> RetrieveByNameAsync(string queueName) =>
        ValueTask.FromResult(this.queues.GetValueOrDefault(queueName));

    public ValueTask RegisterAsync(HoldingBridge holdingBridge)
    {
        this.queues[holdingBridge.QueueName] = holdingBridge;
        return ValueTask.CompletedTask;
    }
}
```

A minimal `ILoggingBroker` (every foundation service needs one — logs validation and
dependency failures):

```csharp
using Ringly.Asterisk.Brokers;

public class ConsoleLoggingBroker : ILoggingBroker
{
    public ValueTask LogInformationAsync(string message) => Log(message);
    public ValueTask LogTraceAsync(string message) => Log(message);
    public ValueTask LogDebugAsync(string message) => Log(message);
    public ValueTask LogWarningAsync(string message) => Log(message);
    public ValueTask LogErrorAsync(Exception exception) => Log(exception.Message);
    public ValueTask LogCriticalAsync(Exception exception) => Log(exception.Message);

    private static ValueTask Log(string message)
    {
        Console.WriteLine(message);
        return ValueTask.CompletedTask;
    }
}
```

## 4. Register everything

```csharp
using Microsoft.Extensions.DependencyInjection;
using Ringly.Abstractions;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Services.Foundations.CallSessions;
using Ringly.Asterisk.Services.Foundations.SipEndpoints;
using Ringly.Asterisk.Services.Processings.Provisioning;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

var services = new ServiceCollection();

services.Configure<AsteriskOptions>(options =>
{
    options.BaseUrl = "http://localhost:8088";
    options.Username = "ringly";
    options.Password = "ringly-dev-ari";
    options.StasisAppName = "ride_hailing_app";
    options.DialplanContext = "ride_hailing";
});

services.AddSingleton<ILoggingBroker, ConsoleLoggingBroker>();

// AsteriskBroker owns a persistent ARI WebSocket + AMI TCP connection — register
// as a singleton, not scoped/transient.
services.AddSingleton<IAsteriskBroker, AsteriskBroker>();

services.AddSingleton<ISipCredentialsStore, InMemorySipCredentialsStore>();
services.AddSingleton<IQueueRegistry, InMemoryQueueRegistry>();

services.AddScoped<IAsteriskSipEndpointConfigFoundationService, AsteriskSipEndpointConfigFoundationService>();
services.AddScoped<ICallProvisioningService, CallProvisioningService>();
services.AddScoped<ICallProvider, AsteriskCallFoundationService>();
services.AddScoped<ICallCenterProvider, AsteriskCallCenterFoundationService>();

var provider = services.BuildServiceProvider();
```

## 5. Provision a client, create a queue, start a call

```csharp
using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;

using var scope = provider.CreateScope();
var provisioningService = scope.ServiceProvider.GetRequiredService<ICallProvisioningService>();
var callProvider = scope.ServiceProvider.GetRequiredService<ICallProvider>();
var callCenterProvider = scope.ServiceProvider.GetRequiredService<ICallCenterProvider>();

var clientId = Guid.NewGuid();

// Generates a random extension + password and provisions it against Asterisk's
// dynamic PJSIP config (realtime backend required — see docker/README.md).
SipCredentials credentials = await provisioningService.AddClientCredentialsAsync(clientId);

// Register the credentials so RouteToQueueAsync can look them up later — this is
// application code, not part of Ringly (see step 3's InMemorySipCredentialsStore).
((InMemorySipCredentialsStore)scope.ServiceProvider.GetRequiredService<ISipCredentialsStore>())
    .Add(credentials);

// Creates a holding bridge in Asterisk and registers it in IQueueRegistry.
HoldingBridge queue = await callCenterProvider.CreateQueueAsync(
    new QueueConfig { Name = "support" });

// Two known parties — e.g. a rider and a driver already matched by your app.
CallSession session = await callProvider.StartCallSessionAsync(
    partyA: new CallParticipant { SipExtension = "1000" },
    partyB: new CallParticipant { SipExtension = credentials.Extension });

Console.WriteLine($"Call session {session.CallSessionId}, bridge {session.BridgeId}");

// Cold entry point — customer taps "contact support" with no active call yet.
CallSession supportSession = await callProvider.RouteToQueueAsync(clientId, "support");

Console.WriteLine($"Routed into queue, customer channel {supportSession.CustomerChannelId}");
```

That's the whole loop: provisioning → queues → calls, all through interfaces your
app depends on, backed by a real Asterisk instance underneath. Swapping to Twilio
means referencing `Ringly.Twilio`/`Ringly.CallCenter.Twilio` instead and registering
their implementations against the same `ICallProvider`/`ICallCenterProvider`
interfaces — see [docs/call-provider.md](call-provider.md) and
[docs/call-center.md](call-center.md).

## Next steps

- [docs/call-provider.md](call-provider.md) — `ICallProvider` on Asterisk and Twilio
- [docs/call-center.md](call-center.md) — queues and transfers
- [docs/trunking.md](trunking.md) — PSTN trunking, spend limits, masked calling
- [docs/client.md](client.md) — client-side (MAUI) calling with SIPSorcery
- [docs/storage.md](storage.md) — call recording storage
- [docs/ai-agent.md](ai-agent.md) — AI voice agents (Twilio ConversationRelay)
