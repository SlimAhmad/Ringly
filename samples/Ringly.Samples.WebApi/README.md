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

## Known limitation

`/clients/{clientId}/provision` will fail against a real Asterisk instance with
`"Cannot create sorcery objects of type 'endpoint'"` — this is a confirmed,
still-open upstream Asterisk bug
([asterisk/asterisk#1655](https://github.com/asterisk/asterisk/issues/1655)):
`res_config_pgsql` never quotes SQL identifiers, so PJSIP `endpoint` object
creation via realtime Postgres always fails. `aor`/`auth`/`identify` creation
works fine; only `endpoint` is affected. No application-level workaround exists.
