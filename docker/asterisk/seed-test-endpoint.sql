-- §6 item 8 smoke test — seeds extensions "1000" and "1001" directly into the
-- realtime tables (ps_endpoints/ps_auths/ps_aors), exercising the same tables
-- row #7's InsertSipEndpointConfigAsync writes to via the ARI dynamic config
-- PUT, without requiring the broker code to be running. Two endpoints (not
-- just one) so samples/Ringly.Samples.Maui's two-instance call demo has a
-- second, ready-to-use extension without depending on the dynamic
-- provisioning path — see docs/getting-started.md's/row #21's note that
-- endpoint creation via the ARI PUT is blocked by a confirmed upstream
-- Asterisk bug (asterisk/asterisk#1655); seeding directly into ps_endpoints
-- like this sidesteps that entirely.
-- ON CONFLICT DO NOTHING — applied automatically on every `docker compose up -d`
-- (see the migrate service), so it must be safe to run against an
-- already-seeded database, not just a fresh one.
-- remove_existing is essential for repeated local testing: with max_contacts=1 and no
-- remove_existing, a fresh app launch registering from a new local UDP port (normal — the
-- OS picks an ephemeral port each run) gets rejected outright with "403 Forbidden ... will
-- exceed max contacts of 1" as long as the PREVIOUS session's contact hasn't yet expired.
-- remove_existing=yes makes a new registration replace the old one instead of being
-- rejected — the correct behavior for an AOR that only ever has one real device using it.
INSERT INTO ps_aors (id, max_contacts, remove_existing)
VALUES ('1000', 1, 'yes'), ('1001', 1, 'yes'), ('1002', 1, 'yes'), ('1003', 1, 'yes'), ('1004', 1, 'yes'),
       ('supportregistrar', 1, 'yes')
ON CONFLICT (id) DO NOTHING;

INSERT INTO ps_auths (id, auth_type, username, password)
VALUES
    ('1000', 'userpass', '1000', 'ringly-dev-1000'),
    ('1001', 'userpass', '1001', 'ringly-dev-1001'),
    ('1002', 'userpass', '1002', 'ringly-dev-1002'),
    ('1003', 'userpass', '1003', 'ringly-dev-1003'),
    ('1004', 'userpass', '1004', 'ringly-dev-1004'),
    ('supportregistrar', 'userpass', 'supportregistrar', 'ringly-dev-supportregistrar')
ON CONFLICT (id) DO NOTHING;

-- allow includes vp8 alongside the audio codecs — confirmed live that without it, Asterisk's
-- own B2BUA rewrites the video m-line to "m=video 0" (rejected) on the answer it relays back
-- to the caller regardless of what the actual callee negotiated, which SIPSorcery's client
-- library treats as a hard call failure (VideoIncompatible), not just a missing video track.
-- rewrite_contact is essential here: Asterisk runs inside the Docker container's own
-- network namespace, so a client's self-reported Contact header ("127.0.0.1:PORT",
-- accurate from the client's own point of view) means the CONTAINER's loopback to
-- Asterisk, not the Windows host. Without rewrite_contact, Asterisk stores that literal
-- address and later routes brand-new calls straight into its own container's loopback —
-- silently unreachable, hence "confirmed bound and listening, yet zero response to any
-- inbound SIP request". Registration and client-initiated calls are unaffected, since
-- those route responses back via the transaction's actually-observed source address, not
-- the stored Contact. rewrite_contact=yes makes Asterisk store the real observed source
-- (e.g. the Docker bridge gateway address) instead of trusting the client's self-report.
-- rtp_timeout/rtp_timeout_hold are the backstop for a client that crashes or is force-killed
-- without sending any SIP signaling at all (no BYE, no CANCEL) — see
-- AsteriskBroker.Credentials.cs's InsertSipEndpointObjectAsync for the full explanation, including
-- why the original 30/60s values (issue #191/PR #192) turned out to be a false positive that killed
-- every real video call around the 30s mark (issue #203) and were raised to 180/300s.
INSERT INTO ps_endpoints (
    id, context, disallow, allow, auth, aors, webrtc, rewrite_contact,
    set_var, rtp_timeout, rtp_timeout_hold
)
VALUES
    ('1000', 'ride_hailing', 'all', 'opus,ulaw,vp8', '1000', '1000', 'yes', 'yes',
     'PJSIP_TRANSFER_HANDLING()=ari-only', 180, 300),
    ('1001', 'ride_hailing', 'all', 'opus,ulaw,vp8', '1001', '1001', 'yes', 'yes',
     'PJSIP_TRANSFER_HANDLING()=ari-only', 180, 300),
    ('1002', 'dograh_ai', 'all', 'ulaw', '1002', '1002', 'yes', 'yes',
     'PJSIP_TRANSFER_HANDLING()=ari-only', 180, 300),
    ('1003', 'ride_hailing', 'all', 'opus,ulaw,vp8', '1003', '1003', 'yes', 'yes',
     'PJSIP_TRANSFER_HANDLING()=ari-only', 180, 300),
    ('1004', 'ride_hailing', 'all', 'opus,ulaw,vp8', '1004', '1004', 'yes', 'yes',
     'PJSIP_TRANSFER_HANDLING()=ari-only', 180, 300)
ON CONFLICT (id) DO NOTHING;
-- ON CONFLICT DO NOTHING means this reassignment only applies to a fresh volume — an
-- already-seeded Postgres data volume keeps 1002's old ride_hailing/opus,ulaw,vp8 row as-is.
-- Recreate the volume (or run the equivalent UPDATE by hand) to pick this up on an existing
-- local stack.
UPDATE ps_endpoints SET context = 'dograh_ai', allow = 'ulaw' WHERE id = '1002';

-- Row #38f — QueueTransferRegistrarService's own real, always-registered SIP endpoint (see that
-- class's own comment for the full architecture). webrtc=yes (not "no", despite this being a
-- plain Windows/ASP.NET process with no browser involved) - confirmed live as a real bug the
-- first time this was tried: QueueTransferRegistrarService uses SipSorceryCallClient, which
-- always negotiates media via RTCPeerConnection (a WebRTC media stack) regardless of what the
-- call itself is for, so it fundamentally requires DTLS-SRTP. webrtc=no here made Asterisk offer
-- plain "RTP/AVP" with no DTLS fingerprint, which our own client rejected outright with
-- "406 DtlsFingerprintMissing" - same reasoning as every other SipSorceryCallClient-based
-- endpoint above (1000-1004), all of which are also webrtc=yes. No
-- PJSIP_TRANSFER_HANDLING()=ari-only here unlike those endpoints - this one never sends its own
-- transfer/REFER at all (that approach was tried and confirmed to make Dograh's own app tear
-- down defensively - see QueueTransferRegistrarService's own comment), so there's nothing to
-- route through ARI instead of core.
INSERT INTO ps_endpoints (id, context, disallow, allow, auth, aors, webrtc, rewrite_contact)
VALUES ('supportregistrar', 'ride_hailing', 'all', 'ulaw,opus', 'supportregistrar', 'supportregistrar', 'yes', 'yes')
ON CONFLICT (id) DO NOTHING;
