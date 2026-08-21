# Ringly.Samples.Maui

MAUI client using `Ringly.Client.SipSorcery` — see
[docs/client.md](../../docs/client.md). Targets **Android and Windows only**.

This is a real end-to-end call demo, not a stub: register, place a call to
another registered client, answer, hang up — run **two instances** of the app
(e.g. two Windows windows, or a Windows instance + an Android emulator/device),
register each as a different extension, and place a real SIP call from one to
the other through Asterisk. One instance plays "rider," the other "driver."

## Prerequisites

```bash
cd ../../docker && docker compose up -d   # local Asterisk + coturn stack
```

Two SIP extensions to register as — the docker stack seeds both directly (via
`docker/asterisk/seed-test-endpoint.sql`, applied automatically on a fresh
`docker compose up -d`), so no provisioning step is needed for this demo:

| Extension | Password |
|---|---|
| `1000` | `ringly-dev-1000` |
| `1001` | `ringly-dev-1001` |

(`Ringly.Samples.WebApi`'s `POST /clients/{clientId}/provision` endpoint also
exists for provisioning further extensions dynamically, but currently fails
against a real Asterisk instance — see that sample's README for why. The two
extensions above are seeded a different way that isn't affected.)

`MauiProgram.cs`'s `SipSorceryCallOptions.RegistrarHost` defaults to
`localhost:5060` (Asterisk's plain UDP SIP port, `transport-udp` in
`docker/asterisk/config/pjsip.conf`) — see the comment there for what to change
it to on Android (`10.0.2.2` for the emulator, your LAN IP for a real device).

**Why UDP and not WS/WSS**: both were tried and confirmed broken against this
Asterisk setup. `wss:8089` fails with `SSLHandshakeException`/`CertificateException`
on Android — Asterisk's `transport-wss` uses the self-signed dev cert
`docker/asterisk/Dockerfile` generates, and Android's platform TLS stack (like any
correctly-behaving TLS stack) rejects it outright. `ws:8088` fails with an HTTP 404
on the WebSocket handshake — `Ringly.Client.SipSorcery`'s `SIPClientWebSocketChannel`
(from the SIPSorcery library) always connects to the server's root path `/` with no
way to target a sub-path, while Asterisk's WebSocket transport is hardcoded to `/ws`
(`res_http_websocket`) — the two can never agree on a path, confirmed by reading
SIPSorcery's own source. Native clients don't need WS anyway — it exists so browsers
(which lack raw socket access) can carry SIP over HTTP — so plain UDP, which
`Ringly.Client.SipSorcery` also registers a channel for, is the correct choice here
and just works.

## Build

```bash
dotnet build -f net10.0-windows10.0.19041.0
dotnet build -f net10.0-android -r android-arm64
```

Android needs the Android SDK (comes with the `android` MAUI workload) and, to
actually run it, an emulator or device — `dotnet build` alone only compiles and
packages, it doesn't deploy. Use Visual Studio or `dotnet build -t:Run` with a
target device selected to deploy and launch.

**Known issue building/running the Windows target from the CLI**: `dotnet build`
(and `dotnet build -t:Run`) for `net10.0-windows10.0.19041.0` currently fails to
restore with:

```
NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64 with version (= 10.0.11)
```

This is a confirmed upstream `dotnet/maui` bug, not a problem with this project —
see [dotnet/maui#32968](https://github.com/dotnet/maui/issues/32968) and
[dotnet/maui#27471](https://github.com/dotnet/maui/issues/27471): the CLI restore
path for MAUI-on-Windows in .NET 10 requests a Mono runtime pack for `win-x64`
that Microsoft hasn't published. `dotnet workload update`/`dotnet workload restore`
do not fix it — confirmed by testing on this repo (the package version in the
error just moves, e.g. `10.0.10` → `10.0.11`, still unpublished either way). The
**Android** target (same code, same project) builds and packages successfully
end to end via the CLI with no changes, so this is Windows/CLI-specific.

**Visual Studio's own build path works around this** — confirmed: building and
running the Windows target through Visual Studio (open `Samples.slnx`, set
`Ringly.Samples.Maui` as the startup project, select the Windows Machine target,
F5) succeeds, since VS uses different restore machinery than the `dotnet` CLI. If
you don't have Visual Studio, the Android target is the CLI-verified path until
Microsoft publishes the missing package.

## Walkthrough (two instances)

1. Build and run two instances of the Windows app (or one Windows + one Android).
2. In instance A: enter extension `1000`, password `ringly-dev-1000`, tap **Register**.
3. In instance B: enter extension `1001`, password `ringly-dev-1001`, tap **Register**.
4. In instance A: enter instance B's extension in "Extension to call," tap **Call**.
5. Instance B's event log shows `IncomingCall` — tap **Answer**.
6. Both instances now show `CallAnswered` — a real, connected SIP call, ready for
   audio through Asterisk.

## Talk to the AI agent (Dograh, opt-in)

Instead of calling a second instance, register one instance (step 2 above) and dial `1002` —
it's routed to a self-hosted AI voice agent (Dograh) instead of another Ringly client. Needs
`docker compose --profile dograh up -d` and a one-time dashboard setup first; see
[docker/README.md](../../docker/README.md#dograh-ai-agent-row-38c-opt-in). No app code
involved — this is an ordinary call from the app's own perspective.

A call can also arrive from `Ringly.Samples.WebApi`'s `POST /calls` endpoint
(`ICallProvider.StartCallSessionAsync` server-originates channels to both
extensions) — the same Answer/Hangup UI handles that origin path too.

## Verified against a real Android emulator

Registration is confirmed working end to end, not just "should work": built in
Release, installed on a Pixel 9 Pro API 36 emulator, registered as `1000` against
the local Docker Asterisk stack, and independently confirmed on the server side
via `asterisk -rx "pjsip show contacts"` showing a live
`1000/sip:10.0.2.16:57332` binding. Two real bugs surfaced and were fixed along
the way (both in `Ringly.Client.SipSorcery/SipSorceryCallClient.cs`, not this
sample):

- **Call target resolution**: `PlaceCallAsync` was passing the bare extension
  (e.g. `"1001"`) straight to SIPSorcery's `Call()`. With no `@domain` part, its
  URI resolution fell back to legacy dotted-decimal integer parsing — `"1001"`
  silently resolved to IP `0.0.3.233` (1001 read as a raw 32-bit value) and the
  call went nowhere. Fixed by qualifying the target against the same registrar
  host used for registration (`sip:{extension}@{registrarHost}`).
- **Call authentication**: once resolution was fixed, Asterisk challenged the
  INVITE the same way it challenges REGISTER (`Authentication requested when no
  credentials available`) and the call still failed. Fixed by having the client
  remember the credentials from its last successful `RegisterAsync` and reuse
  them to answer the INVITE's auth challenge too.

A third bug surfaced once auth was fixed and the call finally reached SDP
negotiation:

- **Empty SDP offer**: `PlaceCallAsync`/`AnswerCallAsync` created a bare
  `RTCPeerConnection` with no media track added, so the generated offer was
  just `v=0/o=.../s=sipsorcery/t=0 0` — no `m=` line at all. Asterisk correctly
  rejected that with `488 Not Acceptable Here` (there was nothing to
  negotiate). Fixed by adding a `SIPSorceryMedia.Abstractions`-based
  `MediaStreamTrack` (PCMU) to every media session before it's used.

With all three fixes in, a call from a registered `1000` to extension `1001`
(not registered by anything in this test) now correctly reaches Asterisk,
authenticates, negotiates real SDP (`100 Trying`, offer accepted, no more
`488`), and enters Asterisk's `ride_hailing` dialplan → `Stasis` app —
confirmed via `pjsip set logger on`. From there it depends on
`Ringly.Samples.WebApi` running: its `RideHailingCallRouter` (see that
sample's own README) answers and bridges the caller, then originates a
channel to the dialed extension and bridges that in too. Two more real bugs
surfaced and were fixed getting this far:

- **Wrong ARI channel-origination format**: `AsteriskCallFoundationService`
  (used by both `StartCallSessionAsync`/`POST /calls` and the new router) was
  passing bare extensions straight to ARI's `/channels?endpoint=` — Asterisk
  rejects that outright with `400 Invalid endpoint specified`; it needs a
  `Tech/Resource` string like `PJSIP/1000`. A prior code comment claiming this
  was "acceptance-tested" without the prefix was simply wrong — no such test
  existed. Fixed in `Ringly.Asterisk`, with the affected unit tests updated
  to match.
- **Bridging a not-yet-answered channel**: `StartCallSessionAsync` originated
  both channels then immediately tried to bridge them, but ARI rejects
  `bridges/{id}/addChannel` with `"Channel not in Stasis application"` until
  a channel actually answers and enters Stasis — confirmed against a real
  instance. Fixed by waiting for each channel's own `StasisStart` event
  (`IAsteriskBroker.StreamStasisStartEvents()`, new) before bridging it.

Verified: dialing from a registered `1000` to `1001` (with nothing registered
as `1001`) now shows the router answering and bridging the caller, then
cleanly catching and logging the expected origination failure for the
unreachable target — no crash, no silent hang, no more the caller just being
parked with nothing happening. Signaling, routing, media negotiation, and
call bridging are all proven working end to end. A call between two live,
registered instances (over a direct connection, bypassing the playit.gg
tunnel — see below) reached a fully connected media session: SDP negotiated,
ICE connected, DTLS handshake completed, SRTP fingerprint matched — the
deepest point reachable without real audio actually flowing (it then hangs
up after ~30s of RTP silence, which is exactly what happened, since nothing
was generating real audio yet — see "Real audio" below).

## Real audio (SIPSorceryMedia.Windows)

The signaling proof above still didn't answer "shouldn't I hear voice back?"
— it didn't, because nothing was capturing a microphone or playing received
audio. `CreateMediaSession()`'s `MediaStreamTrack` only *declared* PCMU
support so SDP negotiation would succeed; it was never connected to a real
audio device. Fixed for **Windows**: `SipSorceryCallClient` now takes
optional `IAudioSource`/`IAudioSink` (from `SIPSorceryMedia.Abstractions`,
already referenced) via constructor injection, wired to the peer connection's
`OnAudioSourceEncodedSample`/`OnAudioFormatsNegotiated`/`OnRtpPacketReceived`
hooks — resolved via Microsoft.Extensions.DependencyInjection's built-in
"fall back to the parameter's default (`null`) when nothing's registered"
behavior, so this stays a no-op wherever nothing's wired up. `MauiProgram.cs`
constructs a `SIPSorceryMedia.Windows.WindowsAudioEndPoint` (real mic
capture + speaker playback via NAudio) and registers it under both
interfaces, `#if WINDOWS` only — that package needs a Windows-specific TFM
`Ringly.Client.SipSorcery` (cross-platform) can't take on itself.

`ICallClient` also gained `MuteAsync`/`UnmuteAsync` (pauses/resumes the
injected `IAudioSource`; a no-op without one).

**Android has no equivalent yet** — there's no official `SIPSorceryMedia.Android`
package. The real fix is a hand-written audio source/sink using Android's
native `AudioRecord`/`AudioTrack` APIs (plus the `RECORD_AUDIO` runtime
permission) — not yet built, tracked as the next concrete step for that side.
[`Plugin.Maui.Audio`](https://github.com/jfversluis/Plugin.Maui.Audio)'s
`AudioStreamer` would solve the *capture* half cross-platform (confirmed: it
exposes a real-time raw-PCM callback, not just record-to-file) — but not
playback, which only supports finished files/streams, not a live incoming
RTP-derived PCM feed. Android would still need its own `AudioTrack`-based
sink either way.

Until Android has a real audio path, calls to/from an Android instance stay
signaling-only (connects and negotiates correctly, but silent both ways) —
the Windows↔Windows path is the one to use for testing actual voice.

## Call screen

`CallPage` now has a dedicated in-call view (not just an event log) — shown
while dialing, ringing (incoming), or connected: an avatar-initial circle,
call state ("Calling" / "Incoming call" / "In call with"), the remote
extension, a live `mm:ss` timer once answered, and Speaker/End/Mute (or
Answer/Decline while incoming) buttons. Mute is real (see above); Speaker is
currently UI-only — there's no speaker/earpiece routing concept on Windows
desktop audio, and Android has no audio device to route yet either.

## Testing from a real, remote device

An Android emulator reaches this stack via `10.0.2.2` and a device on the same
LAN via your machine's IP (see the comment in `MauiProgram.cs`) — for a device
on a different network entirely, see
[docker/README.md](../../docker/README.md#tunneling-for-a-real-remote-device-opt-in)
for the playit.gg tunnel setup (not ngrok — confirmed incompatible with this
stack: ngrok has no UDP support at any tier, and this Asterisk image has no
SIP-over-TCP support compiled in either).
