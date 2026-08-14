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
VALUES ('1000', 1, 'yes'), ('1001', 1, 'yes')
ON CONFLICT (id) DO NOTHING;

INSERT INTO ps_auths (id, auth_type, username, password)
VALUES
    ('1000', 'userpass', '1000', 'ringly-dev-1000'),
    ('1001', 'userpass', '1001', 'ringly-dev-1001')
ON CONFLICT (id) DO NOTHING;

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
INSERT INTO ps_endpoints (
    id, context, disallow, allow, auth, aors, webrtc, rewrite_contact,
    set_var
)
VALUES
    ('1000', 'ride_hailing', 'all', 'opus,ulaw', '1000', '1000', 'yes', 'yes',
     'PJSIP_TRANSFER_HANDLING()=ari-only'),
    ('1001', 'ride_hailing', 'all', 'opus,ulaw', '1001', '1001', 'yes', 'yes',
     'PJSIP_TRANSFER_HANDLING()=ari-only')
ON CONFLICT (id) DO NOTHING;
