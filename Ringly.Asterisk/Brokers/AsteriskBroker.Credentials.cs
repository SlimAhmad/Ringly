using Npgsql;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string PjsipConfigRelativeUrlFormat = "asterisk/config/dynamic/res_pjsip/{0}/{1}";
    private const string TransferHandlingSetVar = "PJSIP_TRANSFER_HANDLING()=ari-only";

    public async ValueTask<IReadOnlyList<ConfigTuple>> RetrieveSipEndpointConfigAsync(string extension) =>
        await this.GetPjsipConfigAsync("aor", extension);

    public async ValueTask InsertSipEndpointConfigAsync(SipEndpointConfig config)
    {
        await this.PutPjsipConfigAsync("aor", config.Extension,
        [
            new ConfigTuple { Attribute = "max_contacts", Value = "1" }
        ]);

        await this.PutPjsipConfigAsync("auth", config.Extension,
        [
            new ConfigTuple { Attribute = "auth_type", Value = "userpass" },
            new ConfigTuple { Attribute = "username", Value = config.Extension },
            new ConfigTuple { Attribute = "password", Value = config.Password }
        ]);

        await this.InsertSipEndpointObjectAsync(config);
    }

    // Direct Postgres write, bypassing ARI's PUT entirely for this one object type — confirmed
    // via a real acceptance-test run that res_config_pgsql fails to quote SQL identifiers like
    // the column "100rel" (asterisk/asterisk#1655), which makes ARI's dynamic-config PUT for
    // "endpoint" fail unconditionally regardless of what fields are sent. Writes directly into
    // the same ps_endpoints table Asterisk's own realtime lookup reads from — same technique
    // docker/asterisk/seed-test-endpoint.sql already uses for local-dev seeding, now the real
    // production path. Column list matches that seed script's full working set (aor/auth's ARI
    // PUTs above don't hit this bug, so they're untouched).
    private async ValueTask InsertSipEndpointObjectAsync(SipEndpointConfig config)
    {
        await using var connection = new NpgsqlConnection(this.asteriskOptions.DatabaseConnectionString);
        await connection.OpenAsync();

        // webrtc/rewrite_contact are the Asterisk realtime schema's custom "ast_bool_values"
        // domain type, not plain text — confirmed live that Npgsql sending them as an ordinary
        // parameterized text value (its default for a C# string) fails with "column is of type
        // ast_bool_values but expression is of type text". The seed script's raw SQL literals
        // don't hit this since Postgres implicitly coerces string literals, but parameters carry
        // an explicit type OID that blocks the same implicit coercion — casting the parameter
        // itself in the query text is the standard fix without registering the domain type with
        // Npgsql.
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO ps_endpoints
                (id, context, disallow, allow, auth, aors, webrtc, rewrite_contact, set_var, rtp_timeout, rtp_timeout_hold)
            VALUES
                (@id, @context, @disallow, @allow, @auth, @aors, @webrtc::ast_bool_values, @rewrite_contact::ast_bool_values,
                 @set_var, @rtp_timeout, @rtp_timeout_hold)
            ON CONFLICT (id) DO UPDATE SET
                context = EXCLUDED.context,
                disallow = EXCLUDED.disallow,
                allow = EXCLUDED.allow,
                auth = EXCLUDED.auth,
                aors = EXCLUDED.aors,
                webrtc = EXCLUDED.webrtc,
                rewrite_contact = EXCLUDED.rewrite_contact,
                set_var = EXCLUDED.set_var,
                rtp_timeout = EXCLUDED.rtp_timeout,
                rtp_timeout_hold = EXCLUDED.rtp_timeout_hold
            """,
            connection);

        command.Parameters.AddWithValue("id", config.Extension);
        command.Parameters.AddWithValue("context", this.asteriskOptions.DialplanContext);
        command.Parameters.AddWithValue("disallow", "all");

        // vp8 added alongside the audio codecs — confirmed live (via `pjsip set logger on` and a
        // real two-device Windows<->Android video call) that without it here, Asterisk's own
        // B2BUA rewrites the video m-line to "m=video 0" (rejected) on every answer it relays
        // back to the caller, even when the actual callee negotiated VP8 successfully end to end.
        // SIPSorcery's SetRemoteDescription then treats that as VideoIncompatible and hangs up
        // the entire call outright (not just the video track) — this is what was producing the
        // "call disconnects the instant the other side answers" symptom on both platforms, not a
        // client-side bug in either CustomWindowsVideoEndPoint or AndroidVideoEndPoint.
        command.Parameters.AddWithValue("allow", "opus,ulaw,vp8");
        command.Parameters.AddWithValue("auth", config.Extension);
        command.Parameters.AddWithValue("aors", config.Extension);
        command.Parameters.AddWithValue("webrtc", this.asteriskOptions.UseWebRtcTransport ? "yes" : "no");
        command.Parameters.AddWithValue("rewrite_contact", "yes");
        command.Parameters.AddWithValue("set_var", TransferHandlingSetVar);

        // Backstop for a genuinely crashed/force-killed client — one that sends no BYE, no CANCEL,
        // nothing at all, so RideHailingCallRouter's StasisEnd-driven peer cleanup (see that class)
        // never fires because Asterisk itself never learns the channel ended. Without this, such a
        // channel (and, once bridged, its peer) sits open indefinitely — confirmed live: 14 leaked
        // channels accumulated over one session, some over an hour old, until an endpoint accrued
        // enough of them that Asterisk started instantly declining every new call to it.
        //
        // 30/60s (this value's original setting, issue #191/PR #192) turned out to be a false
        // positive, not just a tight-but-correct threshold — confirmed live (issue #203) via
        // Asterisk's own log: "Disconnecting channel ... for lack of video RTP activity in 33
        // seconds" on a call whose Android-side app log showed continuous video encoding the whole
        // time, with only 1 total audio frame ever conceded across the entire call. Two consecutive
        // real calls both got killed within a second of each other's duration (~29s), which is a
        // systematic cutoff, not the kind of call genuinely abandoned clients would exhibit — a
        // truly crashed client stays dead for minutes, not seconds. 180/300s gives real calls (and
        // Android's own throttled, sparse video RTP — TargetEncodeFrameRate=15, often achieving
        // only ~3fps in practice) far more headroom while still eventually reaping abandoned ones.
        command.Parameters.AddWithValue("rtp_timeout", 180);
        command.Parameters.AddWithValue("rtp_timeout_hold", 300);

        await command.ExecuteNonQueryAsync();
    }

    // Confirmed working against real ARI: unlike the PUT/insert path, which is blocked for the
    // "endpoint" object type by a confirmed upstream Asterisk bug (res_config_pgsql doesn't quote
    // SQL identifiers like "100rel" — see InsertSipEndpointConfigAsync), DELETE isn't affected by
    // that bug. One object type per call (not a bundled 3-step remove like Insert) — confirmed via
    // a real acceptance-test run that "endpoint" 404s (it was never created, given the PUT bug
    // above), and per the broker rule that brokers must not handle exceptions, this layer cannot
    // itself tolerate that and continue on to auth/aor — that orchestration + idempotent-delete
    // tolerance belongs in AsteriskSipEndpointConfigFoundationService instead.
    public async ValueTask RemoveSipEndpointConfigObjectAsync(string objectType, string extension)
    {
        string relativeUrl = string.Format(PjsipConfigRelativeUrlFormat, objectType, Uri.EscapeDataString(extension));
        await this.DeleteAsync(relativeUrl);
    }

    private async ValueTask PutPjsipConfigAsync(string objectType, string id, IReadOnlyList<ConfigTuple> fields)
    {
        string relativeUrl = string.Format(PjsipConfigRelativeUrlFormat, objectType, Uri.EscapeDataString(id));
        await this.PutAsync(relativeUrl, new PjsipConfigRequestBody { Fields = fields });
    }

    private async ValueTask<IReadOnlyList<ConfigTuple>> GetPjsipConfigAsync(string objectType, string id)
    {
        string relativeUrl = string.Format(PjsipConfigRelativeUrlFormat, objectType, Uri.EscapeDataString(id));
        return await this.GetAsync<List<ConfigTuple>>(relativeUrl);
    }
}
