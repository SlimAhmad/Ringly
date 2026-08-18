# Client-Side Calling (MAUI)

`Ringly.Client.Abstractions` defines the client-side calling contract — this is what
runs on the caller's device (a MAUI app), separate from the server-side
`ICallProvider` in [call-provider.md](call-provider.md). It's deliberately
independent of `Ringly.Abstractions` — different concerns, different lifecycle.

```csharp
public partial interface ICallClient
{
    ValueTask RegisterAsync(SipCredentials credentials);
    ValueTask<CallHandle> PlaceCallAsync(string targetExtension, bool includeVideo = true);
    ValueTask AnswerCallAsync(CallHandle handle);
    ValueTask HangupAsync(CallHandle handle);
    IObservable<CallClientEvent> StreamEvents();
}
```

Models: `SipCredentials { Guid ClientId; string Extension; string Password }` (its
own type in `Ringly.Client.Abstractions.Models` — distinct from the server-side
`Ringly.Abstractions.Models.SipCredentials`), `CallHandle { string Id }`,
`CallClientEvent { string EventType; CallHandle Handle; DateTimeOffset OccurredDate }`.

## SIPSorcery — `Ringly.Client.SipSorcery`

SIP-over-WSS transport + `RTCPeerConnection` as the WebRTC-capable media session.
Works in MAUI, Blazor Server, console, or any .NET app with normal socket access —
**not** Blazor WebAssembly (no raw UDP in-browser).

```csharp
dotnet add package Ringly.Client.SipSorcery
```

```csharp
services.Configure<SipSorceryCallOptions>(options =>
{
    options.RegistrarHost = "your-asterisk-host"; // e.g. the WSS-facing hostname from docker/README.md
    options.RegistrationExpirySeconds = 120;

    // STUN/TURN — see docker/README.md's coturn section for the local dev values
    // (ringly / ringly-dev-turn, realm ringly.local).
    options.IceServerUrls = ["turn:your-coturn-host:3478"];
    options.IceServerUsername = "ringly";
    options.IceServerCredential = "ringly-dev-turn";
});

services.AddSingleton<ICallClient, SipSorceryCallClient>();
```

```csharp
public SipSorceryCallClient(IOptions<SipSorceryCallOptions> options)
```

Usage — register with the SIP extension provisioned server-side via
`ICallProvisioningService.AddClientCredentialsAsync` (see
[call-provider.md](call-provider.md)), then place/answer calls:

```csharp
using Ringly.Client.Abstractions;
using Ringly.Client.Abstractions.Models;

var callClient = provider.GetRequiredService<ICallClient>();

callClient.StreamEvents().Subscribe(clientEvent =>
    Console.WriteLine($"{clientEvent.EventType} on {clientEvent.Handle.Id}"));

await callClient.RegisterAsync(new SipCredentials
{
    ClientId = clientId,
    Extension = "1001",
    Password = "..." // from the server-provisioned SipCredentials
});

CallHandle handle = await callClient.PlaceCallAsync("1000");
// ... later
await callClient.HangupAsync(handle);
```

Platform-specific audio I/O (microphone/speaker capture) is left to the consuming
app — `SipSorceryCallClient` wires the SIP signaling and `RTCPeerConnection`, not
device audio plumbing.
