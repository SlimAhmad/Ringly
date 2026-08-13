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

Two SIP extensions to register as — the docker stack seeds `1000`/`ringly-dev-1000`;
provision a second one via `Ringly.Samples.WebApi`'s `POST /clients/{clientId}/provision`,
or add a second static entry yourself.

`MauiProgram.cs`'s `SipSorceryCallOptions.RegistrarHost` defaults to
`localhost:8089;transport=wss` (Asterisk's WSS port) — see the comment there for
what to change it to on Android (`10.0.2.2` for the emulator, your LAN IP for a
real device).

## Build

```bash
dotnet build -f net10.0-windows10.0.19041.0
dotnet build -f net10.0-android -r android-arm64
```

Android needs the Android SDK (comes with the `android` MAUI workload) and, to
actually run it, an emulator or device — `dotnet build` alone only compiles and
packages, it doesn't deploy. Use Visual Studio or `dotnet build -t:Run` with a
target device selected to deploy and launch.

**Known issue on this dev machine**: the Windows target currently fails to
*restore* (not a code problem) with:

```
NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64 with version (= 10.0.10)
```

This is a mismatch between the installed .NET SDK (`10.0.302`) and the
`maui-windows` workload's expected runtime pack version — confirmed by the fact
that the **Android** target (same code, same project) builds and packages
successfully end to end with no changes. Likely fixed by `dotnet workload update`;
not run here since it's a machine-wide SDK change. If you hit this, that's the
first thing to try.

## Walkthrough (two instances)

1. Build and run two instances of the Windows app (or one Windows + one Android).
2. In instance A: enter extension `1000`, password `ringly-dev-1000`, tap **Register**.
3. In instance B: enter a second provisioned extension/password, tap **Register**.
4. In instance A: enter instance B's extension in "Extension to call," tap **Call**.
5. Instance B's event log shows `IncomingCall` — tap **Answer**.
6. Both instances now show `CallAnswered` — a real, connected SIP call, ready for
   audio through Asterisk.

A call can also arrive from `Ringly.Samples.WebApi`'s `POST /calls` endpoint
(`ICallProvider.StartCallSessionAsync` server-originates channels to both
extensions) — the same Answer/Hangup UI handles that origin path too.
