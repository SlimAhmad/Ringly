# Ringly

Ringly is a pluggable, real-time calling library for .NET/MAUI apps. It gives you a
stable set of interfaces for call handling, call centers, SIP trunking, client-side
calling, recording storage, and AI voice agents — and lets you swap the backend that
implements them (Asterisk, Twilio, self-hosted, managed) without touching your app
code.

Built following [The Standard](https://github.com/hassanhabib/The-Standard)'s
layered architecture and TDD discipline.

## Why "pluggable"

Every capability is defined as an interface in an `*.Abstractions` package, with one
or more backend packages implementing it:

| Interface | Asterisk | Twilio |
|---|---|---|
| `ICallProvider` (start a call, route to a queue) | `Ringly.Asterisk` | `Ringly.Twilio` |
| `ICallCenterProvider` (queues, transfers) | `Ringly.CallCenter.Asterisk` | `Ringly.CallCenter.Twilio` |
| `ISipTrunkProvider` (PSTN trunking, spend limits) | `Ringly.Trunking.Asterisk` | — |
| `IAiVoiceAgentProvider` (AI voice agents) | — | `Ringly.AiAgent.Twilio` |

Your app depends on the `*.Abstractions` interface and picks a backend package at
startup, via ordinary DI registration. Nothing else in your app needs to know or
care which backend is behind it.

## Architecture

Packages follow The Standard's layering:

- **Brokers** (`IAsteriskBroker`, `ITwilioBroker`, `ISipTrunkBroker`, ...) — thin
  wrappers over the real external API/protocol (Asterisk ARI/AMI, Twilio REST API).
  Not unit-tested; verified against the real thing.
- **Foundation services** (`AsteriskCallFoundationService`, `TwilioCallProvider`, ...)
  — one broker each, validation + exception mapping, fully unit-tested. These are
  what implement the public `*.Abstractions` interfaces.
- **Processing / Orchestration services** — compose multiple foundation services for
  higher-level flows (e.g. `MaskedCallOrchestrationService`, `RecordingOrchestrationService`).

## Packages

| Package | Purpose | Docs |
|---|---|---|
| `Ringly.Abstractions` | Core contracts: `ICallProvider`, `ICallProvisioningService`, `ISipCredentialsStore` | [docs/call-provider.md](docs/call-provider.md) |
| `Ringly.Asterisk` | Asterisk/ARI+AMI implementation of the core contracts | [docs/call-provider.md](docs/call-provider.md) |
| `Ringly.Twilio` | Twilio implementation of the core contracts + inbound webhook receiver | [docs/call-provider.md](docs/call-provider.md) |
| `Ringly.CallCenter.Abstractions` | `ICallCenterProvider`, `IQueueRegistry` — queues and transfers | [docs/call-center.md](docs/call-center.md) |
| `Ringly.CallCenter.Asterisk` | Asterisk implementation (holding bridges) | [docs/call-center.md](docs/call-center.md) |
| `Ringly.CallCenter.Twilio` | Twilio implementation (TaskRouter TaskQueues) | [docs/call-center.md](docs/call-center.md) |
| `Ringly.Trunking.Abstractions` | `ISipTrunkProvider` — PSTN trunking, spend/concurrency limits | [docs/trunking.md](docs/trunking.md) |
| `Ringly.Trunking.Asterisk` | Asterisk implementation, masked-call orchestration, spend alerting | [docs/trunking.md](docs/trunking.md) |
| `Ringly.Client.Abstractions` | `ICallClient` — client-side (MAUI) calling contract | [docs/client.md](docs/client.md) |
| `Ringly.Client.SipSorcery` | SIPSorcery-based `ICallClient` implementation (WebRTC-capable) | [docs/client.md](docs/client.md) |
| `Ringly.Storage.Abstractions` | `IRecordingStorageProvider` — call recording storage | [docs/storage.md](docs/storage.md) |
| `Ringly.Storage.AzureBlob` | Azure Blob Storage implementation | [docs/storage.md](docs/storage.md) |
| `Ringly.AiAgent.Abstractions` | `IAiVoiceAgentProvider` — AI voice agent sessions | [docs/ai-agent.md](docs/ai-agent.md) |
| `Ringly.AiAgent.Twilio` | Twilio ConversationRelay implementation | [docs/ai-agent.md](docs/ai-agent.md) |

## Prerequisites

- .NET 10 SDK
- Docker Desktop (optional — only needed to run a local Asterisk/coturn/Dograh dev
  stack for testing; see [docker/README.md](docker/README.md))

## Getting started

See **[docs/getting-started.md](docs/getting-started.md)** for a full worked
example: registering the Asterisk backend, provisioning a client, creating a call
center queue, and starting a call.

Each package group also has its own doc with a DI registration snippet and minimal
usage example — see the table above.

## Local development stack

`docker/README.md` documents a full local Asterisk + coturn (STUN/TURN) stack, plus
an optional self-hosted Dograh AI agent stack, for testing against the real thing
rather than mocks:

```bash
cd docker
docker compose up -d
```
