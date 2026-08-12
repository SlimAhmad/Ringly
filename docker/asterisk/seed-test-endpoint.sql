-- §6 item 8 smoke test — seeds the extension "1000" endpoint directly into the
-- realtime tables (ps_endpoints/ps_auths/ps_aors), exercising the same tables
-- row #7's InsertSipEndpointConfigAsync writes to via the ARI dynamic config
-- PUT, without requiring the broker code to be running.
INSERT INTO ps_aors (id, max_contacts)
VALUES ('1000', 1);

INSERT INTO ps_auths (id, auth_type, username, password)
VALUES ('1000', 'userpass', '1000', 'ringly-dev-1000');

INSERT INTO ps_endpoints (
    id, context, disallow, allow, auth, aors, webrtc,
    set_var
)
VALUES (
    '1000', 'ride_hailing', 'all', 'opus,ulaw', '1000', '1000', 'yes',
    'PJSIP_TRANSFER_HANDLING()=ari-only'
);
