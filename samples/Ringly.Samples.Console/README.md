# Ringly.Samples.Console

Console app on the **Twilio** backend — the counterpart to
`Ringly.Samples.WebApi`'s Asterisk backend, showing both server-side pluggable
backends behind the same `ICallProvider`/`ICallCenterProvider` interfaces. See
[docs/call-provider.md](../../docs/call-provider.md).

## Setup

Needs a real Twilio account. Set these environment variables — never hardcode
them, never commit them:

```bash
export RINGLY_TWILIO_ACCOUNT_SID=AC...
export RINGLY_TWILIO_AUTH_TOKEN=...
export RINGLY_TWILIO_DEFAULT_CALLER_ID=+15551234567   # a number your account owns/verifies
export RINGLY_TWILIO_WORKSPACE_SID=WS...              # a TaskRouter Workspace SID
```

## Run it

```bash
dotnet run -- <partyAPhoneNumber> <partyBPhoneNumber>
```

**This places a real phone call, billed by your Twilio account.** Without
credentials set, or without confirming the `y/N` prompt (or passing `--yes`), it
prints what it would do and exits — it never dials silently.

```bash
dotnet run -- +15551112222 +15553334444 --yes
```
