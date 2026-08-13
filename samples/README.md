# Ringly Samples

Three runnable apps showing Ringly end to end — one per app type, each demonstrating
a different side of the library. They reference the library packages directly via
`ProjectReference` (not published NuGet packages), and live in their own solution
(`samples/Samples.slnx`) so the core `Ringly.slnx` (library + tests) stays untouched.

| Sample | App type | Backend | Demonstrates |
|---|---|---|---|
| [`Ringly.Samples.WebApi`](Ringly.Samples.WebApi) | ASP.NET Core Web API | Asterisk | Server-side `ICallProvider`/`ICallCenterProvider` — provisioning, queues, starting calls, cold-entry routing |
| [`Ringly.Samples.Console`](Ringly.Samples.Console) | Console | Twilio | The **same** interfaces, different backend — the library's core "pluggable" pitch |
| [`Ringly.Samples.Maui`](Ringly.Samples.Maui) | .NET MAUI (Android + Windows) | Asterisk (client) | Client-side `ICallClient` — a real end-to-end call between two registered clients |

See [docs/call-provider.md](../docs/call-provider.md), [docs/call-center.md](../docs/call-center.md),
and [docs/client.md](../docs/client.md) for the interfaces each sample uses.

## Running against the local dev stack

The Web API and MAUI samples are pre-configured for the repo's local Docker
Asterisk/coturn stack:

```bash
cd ../docker
docker compose up -d
```

See [docker/README.md](../docker/README.md) for what that brings up (seeded test
extension `1000`/`ringly-dev-1000`, ARI/AMI/coturn dev credentials).

The console sample needs a **real Twilio account** — see its own README for the
environment variables it expects. It never places a call without an explicit
confirmation.

## Building

```bash
dotnet build samples/Ringly.Samples.WebApi/Ringly.Samples.WebApi.csproj
dotnet build samples/Ringly.Samples.Console/Ringly.Samples.Console.csproj
```

The MAUI sample is platform-specific and isn't included in a plain solution-wide
build — see [`Ringly.Samples.Maui`'s README](Ringly.Samples.Maui/README.md) for its
per-platform build commands.
