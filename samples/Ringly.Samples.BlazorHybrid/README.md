# Ringly.Samples.BlazorHybrid

MAUI Blazor Hybrid client using `Ringly.Client.SipSorcery` — see
[docs/client.md](../../docs/client.md). Targets **Android and Windows only**,
same as `Ringly.Samples.Maui`. Same real end-to-end call demo as that sample —
register, place a call to another registered client, answer, hang up — just
with a Razor UI instead of XAML, and reusing the same platform audio/video
endpoints directly (`Ringly.Samples.Maui`'s `CustomWindowsAudioEndPoint`,
`CustomWindowsVideoEndPoint`, `AndroidAudioEndPoint`, `AndroidCameraXVideoEndPoint`
— linked into this project via `<Compile Include>`, not duplicated).

Full feature parity: register, audio/video calls, answer/decline, hang up,
mute (audio + video), camera switch (Android), a "Request support"
queue-routing flow, and an agent console (availability + live incoming-call
broadcasts + claim).

Built with the-standard-architecture's UI layering throughout: each feature
(call screen, support flow, agent console) is its own view service + Core
Component (`Components/Cores/`), composed on the one page (`Home.razor`).

## Prerequisites

```bash
cd ../../docker && docker compose up -d   # local Asterisk + coturn stack
```

Two SIP extensions to register as — the docker stack seeds both directly, so
no provisioning step is needed for a basic call test:

| Extension | Password |
|---|---|
| `1000` | `ringly-dev-1000` |
| `1001` | `ringly-dev-1001` |

`Ringly.Samples.WebApi` running (`dotnet run` from that sample) if you want to
use the "Request support"/agent-console flows.

## Network configuration

`MauiProgram.cs`'s `CurrentLanHostAddress` constant (marked with a
`// ringly:lan-ip` comment) needs to match your machine's actual LAN IP for
Android device testing and for ICE/TURN to bind the right interface — see
`docker/update-network-ip.ps1` to update it (and `docker/coturn/turnserver.conf`'s
matching `external-ip`) together in one step. See `Ringly.Samples.Maui`'s own
README for the full explanation of why this is needed (emulator vs. real
device vs. Windows all resolve the Asterisk host differently) — this sample
follows the exact same rules, just as its own independent copy (each sample
app is a separate process with its own DI container).

## Styling

Tailwind CSS v4. After editing any `.razor` markup, regenerate `wwwroot/app.css`:

```bash
npm install        # first time only
npm run build:css  # one-shot
npm run watch:css   # watches for changes while developing
```

## Build

```bash
dotnet build -f net10.0-windows10.0.19041.0
dotnet build -f net10.0-android -r android-arm64
```

Android needs the Android SDK (comes with the `android` MAUI workload) and, to
actually run it, an emulator or device — `dotnet build` alone only compiles and
packages, it doesn't deploy.

**Known issue building the Windows target from the CLI**: fails to restore
with `NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64`
— a confirmed upstream `dotnet/maui` bug (see
[`Ringly.Samples.Maui`'s README](../Ringly.Samples.Maui/README.md#build) for
the full writeup and issue links), reproduces identically on the unmodified
MAUI sample, so it's not specific to this project. Visual Studio's own build
path works around it; the Android target builds fine via the CLI either way.

## Walkthrough

1. Build and run two instances (two Windows windows, or Windows + Android).
2. Instance A: register as `1000`/`ringly-dev-1000`.
3. Instance B: register as `1001`/`ringly-dev-1001`.
4. Instance A: enter `1001` as the target extension, tap **📞 Audio call** or
   **📹 Video call**.
5. Instance B's event log shows `IncomingCall` — tap **Answer**.
6. Both instances now show `CallAnswered` — a real, connected call.

For video: Windows renders local preview through a native `CameraView`
overlay on top of the Blazor content (`MainPage.xaml`) since a `BlazorWebView`
can't host a native MAUI control inline; Android's capture is headless
(CameraX directly), so its local preview renders as a throttled `<img>` data
URI in the page instead, same as the remote video on both platforms.

**Support/agent console**: same flow as `Ringly.Samples.BlazorServer`'s own
README describes — see that for the full walkthrough (including the
`agentAppName` == SIP extension convention), identical on this sample. Full
technical details: [WebApi README](../Ringly.Samples.WebApi/README.md#customer-support-walkthrough).

## Verified

Video capture confirmed working against a real machine: startup log showed
`Video capture device Integrated Camera successfully initialised: 1280x720
30fps pixel format NV12.` from `SIPSorceryMedia.Windows.WindowsVideoEndPoint`
(used directly in `Ringly.Samples.BlazorServer`; this sample uses the MAUI
`CameraView`-based `CustomWindowsVideoEndPoint` instead, same underlying VP8
codec). A full two-instance call (audio and video, answered end to end) still
needs a second device/instance to verify live — not yet done as of this
writing.
