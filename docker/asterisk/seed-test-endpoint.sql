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
INSERT INTO ps_aors (id, max_contacts)
VALUES ('1000', 1), ('1001', 1)
ON CONFLICT (id) DO NOTHING;

INSERT INTO ps_auths (id, auth_type, username, password)
VALUES
    ('1000', 'userpass', '1000', 'ringly-dev-1000'),
    ('1001', 'userpass', '1001', 'ringly-dev-1001')
ON CONFLICT (id) DO NOTHING;

INSERT INTO ps_endpoints (
    id, context, disallow, allow, auth, aors, webrtc,
    set_var
)
VALUES
    ('1000', 'ride_hailing', 'all', 'opus,ulaw', '1000', '1000', 'yes',
     'PJSIP_TRANSFER_HANDLING()=ari-only'),
    ('1001', 'ride_hailing', 'all', 'opus,ulaw', '1001', '1001', 'yes',
     'PJSIP_TRANSFER_HANDLING()=ari-only')
ON CONFLICT (id) DO NOTHING;
