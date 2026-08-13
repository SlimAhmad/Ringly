# SIP Trunking

`Ringly.Trunking.Abstractions` defines PSTN trunking contracts — dialing out through
a real phone number provider, inbound masked calling, and spend/concurrency limits.
Asterisk-only in this repo today; there's no Twilio trunking implementation.

```csharp
public partial interface ISipTrunkProvider
{
    ValueTask ConfigureTrunkAsync(SipTrunkConfig config);
    ValueTask RemoveTrunkAsync(string trunkName);
    ValueTask<Channel> DialOutAsync(string phoneNumber, string trunkName);
    IObservable<TrunkCallEvent> StreamInboundTrunkCallsAsync();
    ValueTask<TrunkCallLimitStatus> RetrieveSpendStatusAsync(string trunkName);
}

// App-owned — no implementation ships from Ringly.
public interface IMaskingSessionStore
{
    ValueTask<MaskingSession?> RetrieveByMaskedNumberAsync(string maskedNumber);
}
```

Key models:

```csharp
public class SipTrunkConfig
{
    public string TrunkName { get; set; }
    public string ProviderHost { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public List<string>? AllowedDestinationCountryCodes { get; set; }
    public bool InternationalDialingEnabled { get; set; } = false;
    public decimal? MaxDailySpendUsd { get; set; }
    public int MaxConcurrentCallsPerTrunk { get; set; } = 5;
}

public class MaskingSession
{
    public string MaskedNumber { get; set; }
    public string OtherPartyExtension { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsExpired { get; } // computed from ExpiresAt
}
```

## Asterisk — `Ringly.Trunking.Asterisk`

```csharp
dotnet add package Ringly.Trunking.Asterisk
```

`Ringly.Trunking.Asterisk` keeps its own ARI connection, deliberately separate from
`Ringly.Asterisk`'s — trunking is treated as its own concern with its own broker.

```csharp
services.Configure<SipTrunkOptions>(options =>
{
    options.BaseUrl = "http://localhost:8088";
    options.Username = "ringly";
    options.Password = "ringly-dev-ari";
    options.StasisAppName = "call_center_app";
    options.TrunkDialplanContext = "call_center";
});

services.AddSingleton<Ringly.Trunking.Asterisk.Brokers.ILoggingBroker, ConsoleLoggingBroker>();
services.AddSingleton<ISipTrunkBroker, SipTrunkBroker>(); // owns its own ARI connection — singleton
services.AddScoped<ISipTrunkFoundationService, SipTrunkFoundationService>();
```

```csharp
public interface ISipTrunkFoundationService
{
    ValueTask<Channel> DialOutAsync(string phoneNumber, string trunkName);
}

public SipTrunkFoundationService(
    ISipTrunkBroker sipTrunkBroker,
    ILoggingBroker loggingBroker)
```

`DialOutAsync` validates the destination (country-code allow-list, per-trunk
concurrency and daily spend limits from the trunk's `SipTrunkConfig`) *before*
attempting the dial — provider-side controls remain the primary defense, this is a
belt-and-suspenders app-side layer.

### Masked calling (inbound trunk call → existing call session)

`MaskedCallOrchestrationService` maps an inbound call to a masked number back to the
real other party, using your own `IMaskingSessionStore`:

```csharp
services.AddSingleton<IMaskingSessionStore, /* your implementation */>();
services.AddScoped<IMaskedCallOrchestrationService, MaskedCallOrchestrationService>();
```

```csharp
public MaskedCallOrchestrationService(
    IMaskingSessionStore maskingSessionStore,
    ICallProvider callProvider,        // from Ringly.Abstractions — see call-provider.md
    ILoggingBroker loggingBroker)
```

### Spend alerting

`TrunkSpendAlertService` (unit-testable poll logic) + `TrunkSpendAlertBackgroundService`
(a thin `BackgroundService` wrapper) poll every configured trunk and notify when a
trunk exceeds its concurrency or daily-spend limit:

```csharp
services.Configure<TrunkSpendAlertOptions>(options =>
    options.PollInterval = TimeSpan.FromMinutes(5)); // default

// LoggingTrunkSpendAlertNotifier just logs at Critical — swap in real paging/alerting
// for production.
services.AddSingleton<ITrunkSpendAlertNotifier, LoggingTrunkSpendAlertNotifier>();
services.AddSingleton<ITrunkSpendAlertService, TrunkSpendAlertService>();
services.AddHostedService<TrunkSpendAlertBackgroundService>();
```

### Provider account setup

Configuring a real trunk provider account (spend caps, destination whitelisting)
can't be automated from this repo — see
[docker/trunk-provider-setup.md](../docker/trunk-provider-setup.md) for the checklist.
