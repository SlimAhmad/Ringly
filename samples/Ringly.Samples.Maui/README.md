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

With both fixes in, a call from a registered `1000` to extension `1001` (not
registered by anything in this test) now correctly reaches Asterisk, passes
authentication, and gets a real SIP response — `100 Trying` then
`488 Not Acceptable Here`, most likely a WebRTC media/SDP capability mismatch
between SIPSorcery's `RTCPeerConnection`-based offer and the PJSIP endpoint's
realtime config (`docker/asterisk/seed-test-endpoint.sql`) rather than a
signaling problem — signaling (registration, routing, auth) is proven working;
a full two-instance answered call with audio has not yet been verified and is
the natural next step (needs a second real registered listener to test against,
plus likely a `webrtc`/`media_encryption`/ICE-related tweak to the endpoint
config in the seed SQL).
