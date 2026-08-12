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

## coturn (§6 item 6)

STUN/TURN server (`coturn/coturn:4.17.2-r0-alpine`), covers the client→Asterisk NAT-traversal hop (star topology per §1 — clients don't need to reach each other, only Asterisk).

- `3478` (STUN/TURN, TCP+UDP), `5349` (STUN/TURN over TLS, TCP+UDP), `49160-49200/udp` (relay range)
- Long-term credential mechanism (`lt-cred-mech`), dev-only user `ringly` / `ringly-dev-turn`, realm `ringly.local`
- Self-signed dev TLS cert generated at image build time (same pattern as Asterisk's)

Verified locally: `turnutils_stunclient` returns a reflexive address; `turnutils_uclient` with the dev credentials authenticates and allocates (a same-container loopback peer channel-bind then fails with 403 — coturn's normal `no-loopback-peers` default, not an auth failure); wrong credentials are rejected outright.

**ICE config wiring** — `Ringly.Client.SipSorcery`'s `SipSorceryCallOptions` now has `IceServerUrls`/`IceServerUsername`/`IceServerCredential`; `SipSorceryCallClient` builds an `RTCConfiguration` from them and passes it into every `RTCPeerConnection` it creates.

## Not included yet

- Realtime PJSIP backend (§6 item 8) — Done, see row 19b in Ringly-Reference.md.
- Real TLS certificate — this uses a self-signed dev cert (Asterisk and coturn both).
