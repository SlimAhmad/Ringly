# Ringly.Samples.WebApi

ASP.NET Core Web API on the **Asterisk** backend — see
[docs/call-provider.md](../../docs/call-provider.md) and
[docs/call-center.md](../../docs/call-center.md). MVC controllers only (no minimal
API), with SQL Server-backed telephony identity/device/call-history persistence
(the repo's first storage-based broker) alongside the Asterisk-backed
provisioning/queue/call/recording surface.

## Run it

```bash
cd ../../docker && docker compose up -d   # local Asterisk + coturn + Postgres stack
cd ../samples/Ringly.Samples.WebApi
dotnet run
```

`appsettings.json`'s `Asterisk` section is pre-filled with the docker stack's own
dev credentials (`docker/asterisk/config/ari.conf`) — no edits needed for local use.

`appsettings.json`'s `ConnectionStrings:DefaultConnection` points at a local SQL
Server LocalDB instance (`(localdb)\mssqllocaldb`, database `RinglyTelephony`) for
the `TelephonyIdentity`/`TelephonyDevice`/`TelephonyCall` tables — no Docker
container needed for this part; `StorageBroker`'s constructor calls
`Database.Migrate()` on startup, so the database and schema are created
automatically on first run if LocalDB is installed (it ships with Visual
Studio/the SQL Server Express LocalDB installer).

## Endpoints

All routes are under `api/`. Response codes follow the-standard-architecture's
REST rules: `200`/`201` success, `400` for validation, `404` for not-found,
`409` for already-exists, `500` for dependency/service failures.

### Home

| Method | Route | Does |
|---|---|---|
| GET | `/api/home` | Unauthenticated heartbeat — indicates the API is alive, nothing else |

### Clients (SIP credential provisioning)

| Method | Route | Body | Does |
|---|---|---|---|
| POST | `/api/clients/{clientId}/credentials` | — | Provisions a fresh SIP extension + password for `clientId` |
| GET | `/api/clients/{clientId}/credentials` | — | Retrieves previously-provisioned credentials |
| DELETE | `/api/clients/{clientId}/credentials` | — | Releases the extension |

### Queues & calls

| Method | Route | Body | Does |
|---|---|---|---|
| POST | `/api/queues` | `{ "name": "support", "musicOnHoldClass": "" }` | Creates a holding bridge |
| POST | `/api/calls` | `{ "partyAExtension": "1000", "partyBExtension": "1001" }` | Bridges two known parties directly |
| POST | `/api/support/{clientId}/route?queueName=support` | — | Cold support entry — routes a provisioned client into a queue |

### Telephony identities, devices, call history

| Method | Route | Body | Does |
|---|---|---|---|
| POST | `/api/identities/{identityId}/devices` | `{ "platform": "android" }` | Registers a device under a telephony identity |
| GET | `/api/identities/{identityId}/devices` | — | Lists a telephony identity's registered devices |
| DELETE | `/api/identities/{identityId}/devices/{deviceId}` | — | Unregisters a device |
| GET | `/api/telephonycalls?callerIdentityId=` | — | Call history/CDR (read-only; `callerIdentityId` optional, omit for full history) |
| GET | `/api/telephonycalls/{telephonyCallId}` | — | A single call record |

Call history rows are written automatically as real calls happen — see
`TelephonyCallTrackingService` below, not something a client posts directly.

### Agents (claim / availability / live-call broadcasts)

| Method | Route | Body | Does |
|---|---|---|---|
| POST | `/api/agents/{agentAppName}/availability` | `{ "isAvailable": true }` | Marks an agent available/unavailable |
| POST | `/api/agents/{agentAppName}/claim/{channelId}` | — | Claims a broadcast call for this agent |
| GET | `/api/agents/broadcasts` | — | **Server-Sent Events** stream (`text/event-stream`) of incoming calls needing an agent — stays open until the client disconnects |

### Recordings

| Method | Route | Body | Does |
|---|---|---|---|
| POST | `/api/recordings` | `{ "bridgeId": "...", "recordingName": "...", "format": "wav" }` | Starts recording a bridge |
| POST | `/api/recordings/{recordingName}/pause` | — | Pauses |
| POST | `/api/recordings/{recordingName}/unpause` | — | Resumes |
| POST | `/api/recordings/{recordingName}/stop` | — | Stops (keeps the stored file) |
| POST | `/api/recordings/{recordingName}/cancel` | — | Stops and discards |
| DELETE | `/api/recordings/{recordingName}` | — | Deletes a stored recording |
| POST | `/api/recordings/{recordingName}/copy?destinationName=` | — | Copies a stored recording |

A pause/stop/cancel/delete/copy against a recording that no longer exists returns
`404`, not `500` — Asterisk's own ARI genuinely 404s for that case, since these
operations act on an already-existing resource (contrast `POST /api/queues`,
where a 404 really would mean a misconfigured endpoint).

### Example

```bash
curl -X POST http://localhost:5000/api/queues \
  -H "Content-Type: application/json" \
  -d '{"name":"support"}'
```

Swagger/OpenAPI is available at `/openapi/v1.json` in the Development environment.

## Customer support walkthrough

`POST /api/support/{clientId}/route` and the Agents endpoints above look like two
separate feature areas, but they're actually one flow: a customer requesting
support and an agent claiming them. Here's the real end-to-end sequence,
against any of the Blazor sample apps' Call screen + Support panel + Agent
Console (`Ringly.Samples.BlazorHybrid`/`Ringly.Samples.BlazorServer` — Maui has
no Agent Console):

1. Create a queue once (per queue name you intend to use):
   `POST /api/queues { "name": "support" }`.
2. **Customer**: open the app's Support panel, enter queue name `support`, tap
   "Request support". This provisions a fresh SIP identity, registers it, and
   routes it into the queue — but that means Asterisk originates a real call
   *to the customer's own device* first. **The customer must answer that
   incoming call** (easy to miss, since nothing else prompts for it) before
   they're actually placed on hold in the queue.
3. **Agent**: register under a known SIP extension via the Call screen's
   "Register" panel (there's no auto-provisioning flow for agents the way
   there is for customers — use one of the pre-seeded dev extensions, e.g.
   `1001`). Then open Agent Console, set **"Agent app name" to that exact same
   extension**, and toggle Available.

   This convention — **`agentAppName` IS the agent's own SIP extension** —
   isn't obvious from the UI (there's no separate extension field), but it's
   required: claiming originates a real call to `PJSIP/{agentAppName}`, so the
   agent's own device needs to actually be registered and listening on it.
4. The agent's Agent Console shows the waiting customer via the broadcast
   stream. Tapping claim wins the customer atomically (first agent to claim a
   given call wins; a second attempt gets `409`) and rings a new incoming call
   on the agent's own registered device — answer it to actually talk to the
   customer.

Under the hood this is `SupportQueueBroadcastRegistry` (`Program.cs`,
`AddSingleton`), not `ICallCenterProvider.StreamCallBroadcasts()`/
`ClaimCallAsync()` — those stay wired to Asterisk's own custom
`/ari/events/claim` resource, which is only ever exercised by the
`[broadcast_test]` dialplan context in
`docker/asterisk/config/extensions.conf` (an internal claim-race smoke-test
detail, unrelated to this walkthrough). `SupportController.PostRouteAsync`
publishes into the registry once a customer is genuinely on hold;
`AgentsController` reads from it for `GET /api/agents/broadcasts` and arbitrates
`POST /api/agents/{agentAppName}/claim/{channelId}` atomically before calling
`ICallProvider.ConnectAgentToQueueAsync` to actually bridge the agent in.

## Bridging client-dialed calls (`RideHailingCallRouter`)

`/api/calls` only covers calls *this API* originates. A client dialing another
extension directly (e.g. `samples/Ringly.Samples.BlazorHybrid`/
`Ringly.Samples.BlazorServer`/`Ringly.Samples.Maui` registered as `1000` calling
`1001`) hits `docker/asterisk/config/extensions.conf`'s `[ride_hailing]` context,
which hands the channel to `Stasis(ride_hailing_app, ${EXTEN})` — with nothing
listening for that, the caller would just be left parked with nothing happening.
`RideHailingCallRouter` (a `BackgroundService`, registered in `Program.cs`) fixes
this: it subscribes to `IAsteriskBroker.StreamStasisStartEvents()`, and for any
channel that enters Stasis with dialplan args (a client-dialed call, not one this
API originated itself), it answers the caller, creates a bridge, originates a
channel to the dialed extension, and bridges that in too once it answers.

It also publishes higher-level `CallLifecycleEvent`s (`Initiated`/`Answered`/`Ended`)
that `TelephonyCallTrackingService` (a second `BackgroundService`) consumes to
create/update the `TelephonyCall` rows `GET /api/telephonycalls` returns —
resolving both parties' `TelephonyIdentity` via their SIP extension. A call
between two extensions that were never provisioned through `ClientsController`
still bridges normally; it's just skipped from history (logged, not an error).

## SQL Server persistence

`StorageBroker` (EFxceptions-wrapped `DbContext`) backs `TelephonyIdentity`
(SIP identity tied to an app-level `UserId`), `TelephonyDevice` (per-device
registration), and `TelephonyCall` (call history/CDR). This is entirely
separate from Asterisk's own Postgres-backed realtime PJSIP config — nothing
here touches Asterisk's own lookup path.

## Known limitation (fixed, kept for history)

Endpoint provisioning previously failed against a real Asterisk instance with
`"Cannot create sorcery objects of type 'endpoint'"` — a confirmed upstream
Asterisk bug ([asterisk/asterisk#1655](https://github.com/asterisk/asterisk/issues/1655)):
`res_config_pgsql` never quotes SQL identifiers, so PJSIP `endpoint` object
creation via realtime Postgres always failed through ARI. **This is now worked
around**: `AsteriskBroker` writes the `endpoint` object directly into Postgres's
`ps_endpoints` table (bypassing ARI's PUT for that one object type only — `aor`/
`auth`/`identify` still go through ARI, which works fine for those). See
`AsteriskOptions.DatabaseConnectionString` and `AsteriskBroker.Credentials.cs`.
