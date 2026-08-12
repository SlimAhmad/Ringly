# Local Asterisk Deployment (Ringly-Reference.md §6)

Runs Asterisk 23.4.1 locally via Docker for development/testing against `Ringly.Asterisk`. Satisfies §5.3's `>= 20.20/22.10/23.4` StasisBroadcast version requirement.

## Run it

```bash
cd docker
docker compose up -d
```

## What's configured (§6 checklist items 1–5)

- `pjsip.conf` — `transport-wss` (WebRTC/mobile) + `transport-udp`, plus a static smoke-test endpoint (`1000` / `ringly-dev-1000`) with `webrtc=yes` and `set_var=PJSIP_TRANSFER_HANDLING()=ari-only`
- `extensions.conf` — `ride_hailing`/`call_center` Stasis handoff contexts
- `http.conf` — built-in HTTP server (8088 plain / 8089 TLS), which ARI and WSS both ride on
- `ari.conf` — ARI user `ringly` / `ringly-dev-ari`, `channelvars` for StasisBroadcast routing
- `manager.conf` — AMI user `ringly` / `ringly-dev-ami`, scoped to `call,cdr,agent` (not `system`)
- Self-signed dev TLS cert generated at image build time

**All dev-only credentials** — replace before any non-local deployment.

## Verified locally

```bash
docker compose exec asterisk asterisk -rx "pjsip show endpoint 1000"
docker compose exec asterisk asterisk -rx "http show status"
docker compose exec asterisk asterisk -rx "dialplan show ride_hailing"
docker compose exec asterisk asterisk -rx "manager show users"
```

## Not included yet

- Realtime PJSIP backend (§6 item 8, Postgres/ODBC) — needed for row #7's dynamic config PUT (`InsertSipEndpointConfigAsync`) to actually persist. Tracked as a follow-up using Asterisk's official `contrib/ast-db-manage` Alembic migrations rather than a hand-written schema.
- coturn (STUN/TURN) — row #20.
- Real TLS certificate — this uses a self-signed dev cert.
