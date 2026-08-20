# Ringly.Samples.BlazorServer

Server-rendered ASP.NET Core Blazor app using `Ringly.Client.SipSorcery` directly
— see [docs/client.md](../../docs/client.md). Runs as a normal server process
(like `Ringly.Samples.WebApi`), so it uses real UDP sockets and local audio/video
hardware directly, sidestepping the browser-sandbox problem a true Blazor
WebAssembly client would hit. **True Blazor WASM is explicitly out of scope** —
it would need a different client built on browser WebRTC/`getUserMedia` via JS
interop, not a reuse of `Ringly.Client.SipSorcery`.

Full feature parity with `Ringly.Samples.Maui`/`Ringly.Samples.BlazorHybrid`:
register, place an audio or video call, answer/decline, hang up, mute (audio +
video), plus a "Request support" queue-routing flow and an agent console
(availability toggle + live incoming-call broadcasts + claim).

Built with the-standard-architecture's UI layering throughout: each feature
(call screen, support flow, agent console) is its own view service + Core
Component (`Components/Cores/`), composed on the one page (`Home.razor`).

## Prerequisites

- **Windows** — this sample hand-rolls real microphone/speaker capture via
  NAudio (`CustomWindowsAudioEndPoint`, linked from `Ringly.Samples.Maui`) and
  real webcam capture via `SIPSorceryMedia.Windows.WindowsVideoEndPoint`
  (WinRT `MediaCapture`, no MAUI/CameraView involved) — both Windows-only, so
  the project targets `net10.0-windows10.0.19041.0`.
- The local Docker Asterisk/coturn stack:

  ```bash
  cd ../../docker && docker compose up -d
  ```

- `Ringly.Samples.WebApi` running (`dotnet run` from that sample) if you want
  to use the "Request support"/agent-console flows — they call its
  `ClientsController`/`SupportController`/`AgentsController` over HTTP.

## Run it

```bash
dotnet run --project samples/Ringly.Samples.BlazorServer
```

Opens on `http://localhost:5250` (see `Properties/launchSettings.json`).

`appsettings.json`'s `SipSorceryCall` section points at `localhost` (Asterisk
running alongside on the same machine) and `WebApiClient:BaseUrl` points at
`http://localhost:5000` (`Ringly.Samples.WebApi`'s default port) — edit both if
either runs somewhere else.

**Webcam note**: `WindowsVideoEndPoint.InitialiseVideoSourceDevice()` opens the
default webcam once at process startup, not per-call — the camera indicator
light turns on as soon as this app starts, not only during an active video
call. Fine for a getting-started sample; revisit if that's undesirable for a
real deployment.

## Styling

Tailwind CSS v4. After editing any `.razor` markup, regenerate `wwwroot/app.css`:

```bash
npm install        # first time only
npm run build:css  # one-shot
npm run watch:css   # watches for changes while developing
```

## Walkthrough

1. **Register**: enter an extension/password (e.g. `1000`/`ringly-dev-1000`
   from the docker stack's seeded test endpoint — see
   [docker/README.md](../../docker/README.md)) and tap **Register**.
2. **Call**: enter a target extension, tap **Audio call** or **Video call**.
   Run a second instance of this sample (or `Ringly.Samples.Maui`/
   `Ringly.Samples.BlazorHybrid`) registered as the target extension to
   answer it.
3. **Support**: enter a queue name and tap **Request support** — this
   provisions a fresh identity via `Ringly.Samples.WebApi`'s
   `ClientsController`, registers this app's `ICallClient` with it, then
   routes it into the named queue via `SupportController`. This originates a
   real call *to this app's own newly-registered identity* — **you must
   answer it** like any other incoming call before you're actually placed on
   hold in the queue.
4. **Agent console**: register a second instance of this app (or another
   sample) under a known SIP extension via step 1's Register panel first —
   e.g. `1001`. Then, in the Agent Console, set **"Agent app name" to that
   exact same extension** and toggle **Available**. Nothing in this UI states
   it, but `agentAppName` IS treated as the agent's own SIP extension: claiming
   originates a real call to it. Watch the "Incoming calls" list — customers
   routed into a queue via step 3 appear here in real time, with a **Claim**
   button per entry. Claiming rings a new incoming call on the agent's own
   registered device (the same one from step 1's Register panel) — answer it
   to actually talk to the customer. Full details:
   [WebApi README](../Ringly.Samples.WebApi/README.md#customer-support-walkthrough).

Recording controls during an active claimed call are not wired into this UI
yet — see the `AgentsController`/`RecordingsController` HTTP endpoints in
`Ringly.Samples.WebApi`'s own README if you need them directly.

## Build

```bash
dotnet build samples/Ringly.Samples.BlazorServer/Ringly.Samples.BlazorServer.csproj
```
