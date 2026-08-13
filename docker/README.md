# Local Asterisk Deployment (Ringly-Reference.md §6)

Runs Asterisk 23.4.1 locally via Docker for development/testing against `Ringly.Asterisk`. Satisfies §5.3's `>= 20.20/22.10/23.4` StasisBroadcast version requirement.

## Run it

```bash
cd docker
docker compose up -d
```

## What's configured (§6 checklist items 1–5)

- `pjsip.conf` — `transport-wss` (WebRTC/mobile) + `transport-udp`. No static endpoints —
  those are seeded into the realtime backend instead, see below.
- `extensions.conf` — `ride_hailing`/`call_center` Stasis handoff contexts
- `http.conf` — built-in HTTP server (8088 plain / 8089 TLS), which ARI and WSS both ride on
- `ari.conf` — ARI user `ringly` / `ringly-dev-ari`, `channelvars` for StasisBroadcast routing
- `manager.conf` — AMI user `ringly` / `ringly-dev-ami`, scoped to `call,cdr,agent` (not `system`)
- Self-signed dev TLS cert generated at image build time

**All dev-only credentials** — replace before any non-local deployment.

## Seeded test extensions

PJSIP objects are realtime (backed by Postgres, row #19b), not static `pjsip.conf`
entries. The `migrate` service applies both the schema (Asterisk's own Alembic
migrations) and `docker/asterisk/seed-test-endpoint.sql` automatically on every
`docker compose up -d` (idempotent — safe to run against an already-seeded
database, not just a fresh one). Two ready-to-use extensions, both in the
`ride_hailing` dialplan context:

| Extension | Password |
|---|---|
| `1000` | `ringly-dev-1000` |
| `1001` | `ringly-dev-1001` |

Used by `samples/Ringly.Samples.Maui`'s two-instance call demo. Seeding directly
into the realtime tables like this sidesteps a confirmed upstream Asterisk bug
([asterisk/asterisk#1655](https://github.com/asterisk/asterisk/issues/1655)) that
blocks PJSIP `endpoint` object creation through Asterisk's own dynamic-config ARI
PUT — the path `Ringly.Asterisk`'s `CallProvisioningService` uses, which is why
that path still doesn't work end-to-end against this stack (see
`samples/Ringly.Samples.WebApi`'s README).

## Verified locally

```bash
docker compose exec asterisk asterisk -rx "pjsip show endpoint 1000"
docker compose exec asterisk asterisk -rx "pjsip show endpoint 1001"
docker compose exec asterisk asterisk -rx "http show status"
docker compose exec asterisk asterisk -rx "dialplan show ride_hailing"
docker compose exec asterisk asterisk -rx "manager show users"
```

## coturn (§6 item 6)

STUN/TURN server (`coturn/coturn:4.17.2-r0-alpine`), covers the client→Asterisk NAT-traversal hop (star topology per §1 — clients don't need to reach each other, only Asterisk).

- `3478` (STUN/TURN, TCP+UDP), `5349` (STUN/TURN over TLS, TCP+UDP), `49160-49200/udp` (relay range)
- Long-term credential mechanism (`lt-cred-mech`), dev-only user `ringly` / `ringly-dev-turn`, realm `ringly.local`
- Self-signed dev TLS cert generated at image build time (same pattern as Asterisk's)

Verified locally: `turnutils_stunclient` returns a reflexive address; `turnutils_uclient` with the dev credentials authenticates and allocates (a same-container loopback peer channel-bind then fails with 403 — coturn's normal `no-loopback-peers` default, not an auth failure); wrong credentials are rejected outright.

**ICE config wiring** — `Ringly.Client.SipSorcery`'s `SipSorceryCallOptions` now has `IceServerUrls`/`IceServerUsername`/`IceServerCredential`; `SipSorceryCallClient` builds an `RTCConfiguration` from them and passes it into every `RTCPeerConnection` it creates.

## Tunneling for a real remote device (opt-in)

An Android emulator reaches this stack via `10.0.2.2` and a device on the same LAN via your
machine's IP, but a genuinely remote device (different network entirely) needs a public tunnel.
Use [playit.gg](https://playit.gg), not ngrok — confirmed by testing: ngrok has no UDP support
at any tier, and this Asterisk image's pjproject build has no TCP transport compiled in either
(`pjsip show transports` never lists one no matter what's configured), so the two are
incompatible with each other. playit.gg tunnels UDP directly, matching the transport
`samples/Ringly.Samples.Maui` already uses.

```bash
# one-time: sign up / log in at playit.gg, claim an agent, copy its secret key into docker/.env:
#   PLAYIT_SECRET_KEY=<your key>
docker compose --profile tunnel up -d playit
```

Then, in the playit.gg dashboard, add a UDP tunnel for the agent pointing at local address
`asterisk:5060` (the `playit` container shares this file's default network, so the service name
resolves) — the dashboard shows the public host:port to point a remote MAUI instance's
`RegistrarHost` at. Off by default (opt-in via the `tunnel` profile) since it dials out to a
third-party relay network. Full two-way audio from a genuinely remote device additionally needs
coturn's relay (`3478/udp` + the `49160-49200` range above) reachable too, which needs its own
tunnel/port-forward — not set up here, since the signaling path (register + call routing) is
what this was built to prove.

## SIP trunk provider (§8.7, row #28)

Configuring a real SIP trunk (PSTN masked-calling fallback) needs a real external provider
account that can't be stood up in this repo — see [trunk-provider-setup.md](trunk-provider-setup.md)
for the deployment checklist (provider-side spend cap/whitelist, and why no static `pjsip.conf`
trunk stanza is needed given the realtime PJSIP backend below).

## Dograh AI agent (row #38c, opt-in)

Vendored here so anyone cloning this repo gets a working instance, rather than pointing at a
personal machine-specific one. Off by default — the AI agent path is a stretch feature (§11.5),
not part of the base stack:

```bash
docker compose --profile dograh up -d
```

Brings up Dograh's own images (`ghcr.io/dograh-hq/dograh-api` / `dograh-ui`) plus dedicated
`dograh-postgres`/`dograh-redis`/`dograh-minio` (separate from Ringly's own `postgres` service).
Dashboard: `http://localhost:3010`. API: `http://localhost:8000`. All dev-only credentials —
replace before any non-local deployment. If you already run a separate Dograh instance
elsewhere, stop it first (this profile publishes the same default host ports).

**Why there's no code integration**: row #38b found there's no API for Ringly's own code to hand
Dograh a call it already originated — Dograh always owns call origination itself, on every
integration path it exposes (confirmed against its real docs, not guessed). So Dograh and
Ringly coexist as two independent Stasis applications on the *same* Asterisk PBX instead:

- **Asterisk side** (`ari.conf`): a second ARI app user, `[dograh]` / `dograh-dev-ari`. In
  Dograh's dashboard, its Asterisk ARI telephony configuration needs "Stasis App Name" = `dograh`
  and the matching password, pointed at this Asterisk instance (`asterisk:8088` from inside the
  `dograh` profile's containers — same default Compose network, no extra networking needed).
- **Dialplan** (`extensions.conf`): a `[dograh_ai]` context routing straight to `Stasis(dograh)`.
  Assign whichever extensions you want AI-answered from the start to this context — separate
  from `ride_hailing`/`call_center` above. Calls never move between Dograh's app and Ringly's
  mid-call in either direction.
- **External media** (`websocket_client.conf`): a `[dograh_media]` connection for
  `chan_websocket`/`res_websocket_client` (confirmed loaded in this image — module names
  `chan_websocket.so`, `res_websocket_client.so`, `res_http_websocket.so`,
  `res_pjsip_transport_websocket.so`) — `connection_type = per_call_config` means this is a
  template; Dograh's own ARI app supplies the real per-call target when it creates the
  `externalMedia` channel. Dograh's external media uses G.711 μ-law — set `allow=ulaw` at
  provisioning time for whichever extension you assign to `dograh_ai` (endpoints are
  dynamic/realtime here, row #19b, not static in `pjsip.conf`).
- **Escalation to a human**: a plain `Dial()`/transfer from within Dograh's own workflow into
  one of Ringly's queue extensions — an ordinary inbound call from Dograh's side, no code needed
  on Ringly's.

**Verified locally** (2026-08-13): `chan_websocket`/`res_websocket_client`/`res_http_websocket`/
`res_pjsip_transport_websocket` all loaded cleanly in the live container;
`res_websocket_client.so` reloads without error after adding `websocket_client.conf`; the
`dograh_ai` dialplan context and `[dograh]` ARI user both load correctly
(`--force-recreate --renew-anon-volumes` needed — same `/etc/asterisk` volume gotcha as row
#19b); `docker compose --profile dograh up -d` brings up all 5 services healthy; confirmed
bidirectional container-name connectivity — `dograh-api` reached `asterisk:8088`'s ARI (`200` on
`/ari/asterisk/info` with the `ringly` credentials) and `asterisk` resolved `dograh-api` by DNS.
Not verified: an actual end-to-end call through Dograh's dashboard (needs manual dashboard setup
— Stasis app credentials, extension assignment — that's inherently a UI step).

## Not included yet

- Realtime PJSIP backend (§6 item 8) — Done, see row 19b in Ringly-Reference.md.
- Real TLS certificate — this uses a self-signed dev cert (Asterisk and coturn both).
