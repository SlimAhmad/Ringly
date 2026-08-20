# Ringly Samples

Runnable apps showing Ringly end to end — one per app type/backend combination,
each demonstrating a different side of the library. They reference the library
packages directly via `ProjectReference` (not published NuGet packages), and
live in their own solution (`samples/Samples.slnx`) so the core `Ringly.slnx`
(library + tests) stays untouched.

| Sample | App type | Backend | Demonstrates |
|---|---|---|---|
| [`Ringly.Samples.WebApi`](Ringly.Samples.WebApi) | ASP.NET Core Web API | Asterisk | Server-side `ICallProvider`/`ICallCenterProvider` — provisioning, queues, starting calls, cold-entry routing, agent claim/availability/broadcasts, recordings, SQL Server-backed telephony identity/device/call-history persistence |
| [`Ringly.Samples.Console`](Ringly.Samples.Console) | Console | Twilio | The **same** interfaces, different backend — the library's core "pluggable" pitch |
| [`Ringly.Samples.Maui`](Ringly.Samples.Maui) | .NET MAUI (Android + Windows) | Asterisk (client) | Client-side `ICallClient` — a real end-to-end audio/video call between two registered clients, XAML UI |
| [`Ringly.Samples.BlazorHybrid`](Ringly.Samples.BlazorHybrid) | .NET MAUI Blazor Hybrid (Android + Windows) | Asterisk (client) | Same as `Ringly.Samples.Maui`, Razor UI instead of XAML, reusing the same platform audio/video endpoints directly |
| [`Ringly.Samples.BlazorServer`](Ringly.Samples.BlazorServer) | ASP.NET Core Blazor Server (Windows) | Asterisk (client) | Same client-side call demo as a server-rendered process — real UDP sockets/local hardware, no browser-sandbox workaround needed (true Blazor WASM is explicitly out of scope) |

See [docs/call-provider.md](../docs/call-provider.md), [docs/call-center.md](../docs/call-center.md),
and [docs/client.md](../docs/client.md) for the interfaces each sample uses.

## Running against the local dev stack

The Web API, MAUI, and both Blazor samples are pre-configured for the repo's
local Docker Asterisk/coturn/Postgres stack:

```bash
cd ../docker
docker compose up -d
```

See [docker/README.md](../docker/README.md) for what that brings up (seeded test
extension `1000`/`ringly-dev-1000`, ARI/AMI/coturn dev credentials).

`Ringly.Samples.WebApi` also needs SQL Server LocalDB for its telephony
identity/device/call-history tables (no Docker container — see its own README)
— on first run it creates the database and applies migrations automatically.

The `Ringly.Samples.BlazorHybrid`/`Ringly.Samples.BlazorServer` "Request
support" and agent console flows need `Ringly.Samples.WebApi` running too
(they call it over HTTP).

The console sample needs a **real Twilio account** — see its own README for the
environment variables it expects. It never places a call without an explicit
confirmation.

## Building

```bash
dotnet build samples/Ringly.Samples.WebApi/Ringly.Samples.WebApi.csproj
dotnet build samples/Ringly.Samples.Console/Ringly.Samples.Console.csproj
dotnet build samples/Ringly.Samples.BlazorServer/Ringly.Samples.BlazorServer.csproj
```

The MAUI and MAUI Blazor Hybrid samples are platform-specific and aren't
included in a plain solution-wide build — see
[`Ringly.Samples.Maui`'s](Ringly.Samples.Maui/README.md#build) and
[`Ringly.Samples.BlazorHybrid`'s](Ringly.Samples.BlazorHybrid/README.md#build)
own READMEs for their per-platform build commands.
