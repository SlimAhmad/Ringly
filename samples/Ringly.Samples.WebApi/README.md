# Ringly.Samples.WebApi

ASP.NET Core Web API on the **Asterisk** backend — see
[docs/call-provider.md](../../docs/call-provider.md) and
[docs/call-center.md](../../docs/call-center.md).

## Run it

```bash
cd ../../docker && docker compose up -d   # local Asterisk + coturn stack
cd ../samples/Ringly.Samples.WebApi
dotnet run
```

`appsettings.json`'s `Asterisk` section is pre-filled with the docker stack's own
dev credentials (`docker/asterisk/config/ari.conf`) — no edits needed for local use.

## Endpoints

| Method | Route | Body | Does |
|---|---|---|---|
| POST | `/clients/{clientId}/provision` | — | `AddClientCredentialsAsync` — generates a SIP extension + password |
| POST | `/queues` | `{ "name": "support", "musicOnHoldClass": "" }` | `CreateQueueAsync` — creates a holding bridge |
| POST | `/calls` | `{ "partyAExtension": "1000", "partyBExtension": "1001" }` | `StartCallSessionAsync` — bridges two known parties |
| POST | `/support/{clientId}/route?queueName=support` | — | `RouteToQueueAsync` — cold support entry (needs a prior `/provision` call for that `clientId`) |

Example:

```bash
curl -X POST http://localhost:5000/queues \
  -H "Content-Type: application/json" \
  -d '{"name":"support"}'
```

Swagger/OpenAPI is available at `/openapi/v1.json` in the Development environment.

## Bridging client-dialed calls (`RideHailingCallRouter`)

`/calls` only covers calls *this API* originates. A client dialing another extension directly
(e.g. `samples/Ringly.Samples.Maui` registered as `1000` calling `1001`) hits
`docker/asterisk/config/extensions.conf`'s `[ride_hailing]` context, which hands the channel to
`Stasis(ride_hailing_app, ${EXTEN})` — with nothing listening for that, the caller was just left
parked with nothing happening (confirmed: no ringing, no decline, no timeout).
`RideHailingCallRouter` (a `BackgroundService`, registered in `Program.cs`) fixes this: it
subscribes to `IAsteriskBroker.StreamStasisStartEvents()`, and for any channel that enters Stasis
with dialplan args (i.e. a client-dialed call, not one this API originated itself), it answers
the caller, creates a bridge, originates a channel to the dialed extension, and bridges that in
too once it answers.

Verified against a real Asterisk instance: dialing from a registered `1000` to `1001` (with
nothing registered as `1001`) shows the router answering and bridging the caller's channel, then
cleanly catching and logging the expected `"Allocation failed"` origination error for the
unreachable target — no crash, no silent hang. A fully answered call between two live parties
needs a second real registered client to test against, which needs either a second device or the
Windows build (see `samples/Ringly.Samples.Maui/README.md`'s known CLI build issue) — not yet
verified.

## Known limitation

`/clients/{clientId}/provision` will fail against a real Asterisk instance with
`"Cannot create sorcery objects of type 'endpoint'"` — this is a confirmed,
still-open upstream Asterisk bug
([asterisk/asterisk#1655](https://github.com/asterisk/asterisk/issues/1655)):
`res_config_pgsql` never quotes SQL identifiers, so PJSIP `endpoint` object
creation via realtime Postgres always fails. `aor`/`auth`/`identify` creation
works fine; only `endpoint` is affected. No application-level workaround exists.
