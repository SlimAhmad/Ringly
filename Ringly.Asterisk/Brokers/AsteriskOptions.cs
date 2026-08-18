namespace Ringly.Asterisk.Brokers;

public class AsteriskOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string StasisAppName { get; set; } = string.Empty;
    public string DialplanContext { get; set; } = string.Empty;
    public bool UseWebRtcTransport { get; set; } = true;
    public int AmiPort { get; set; } = 5038;
    public string AmiUsername { get; set; } = string.Empty;
    public string AmiSecret { get; set; } = string.Empty;

    // Direct Postgres access for the "endpoint" PJSIP object only — everything else (aor, auth,
    // and all removal) goes through ARI. Confirmed via a real acceptance-test run that
    // res_config_pgsql fails to quote SQL identifiers like the column "100rel" (a confirmed
    // upstream Asterisk bug, asterisk/asterisk#1655), which makes ARI's PUT for "endpoint"
    // fail unconditionally — this bypasses that specific write by going straight at the same
    // Postgres realtime tables Asterisk itself reads from. Default matches
    // docker/asterisk/config/res_pgsql.conf's dev credentials and docker-compose.yml's
    // published port for this service — 5433, not Postgres's standard 5432, since a native
    // Postgres install can already be running on 5432 on a dev machine (confirmed live: that
    // silently intercepted connections meant for this container, producing a genuine but
    // misleading password-auth failure).
    public string DatabaseConnectionString { get; set; } =
        "Host=localhost;Port=5433;Database=asterisk;Username=asterisk;Password=asterisk";
}
