using System.Collections.Concurrent;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models;
using RESTFulSense.Exceptions;

namespace Ringly.Trunking.Asterisk.Brokers;

public partial class SipTrunkBroker
{
    // §8.4's validation flow needs to read back the business-rule fields of a previously
    // configured trunk (spend caps, allowed destinations, etc.) — Asterisk's PJSIP objects have
    // no concept of these, only raw SIP protocol config, so there's nowhere in Asterisk itself
    // to persist or retrieve them. Not doc-specified where this should live; an in-memory store
    // is a reasonable default for a single-process deployment, but does NOT survive a restart —
    // a durable store (e.g. a database-backed config table) is a real gap, confirm before
    // shipping anything that depends on trunk config surviving process restarts.
    private readonly ConcurrentDictionary<string, SipTrunkConfig> trunkConfigs = new();

    // configClass is "res_pjsip" (the sorcery module name), not "pjsip" — confirmed against
    // Asterisk's own res/ari/resource_asterisk.c (row #21).
    private const string PjsipConfigRelativeUrlFormat = "asterisk/config/dynamic/res_pjsip/{0}/{1}";

    // §8.7 item 12 — trunk endpoint/aor/auth/identify objects, credential-authenticated (the
    // doc leaves exact field choices to the provider's requirements; this is a reasonable
    // default matching common SIP trunk provider setups (e.g. Twilio Elastic SIP Trunking) —
    // not doc-verified against a real trunk provider, confirm before shipping.
    public async ValueTask ConfigureTrunkAsync(SipTrunkConfig config)
    {
        await this.PutPjsipConfigAsync("aor", config.TrunkName,
        [
            new ConfigTuple { Attribute = "contact", Value = $"sip:{config.ProviderHost}" }
        ]);

        await this.PutPjsipConfigAsync("auth", config.TrunkName,
        [
            new ConfigTuple { Attribute = "auth_type", Value = "userpass" },
            new ConfigTuple { Attribute = "username", Value = config.Username },
            new ConfigTuple { Attribute = "password", Value = config.Password }
        ]);

        // "match" requires a valid IP address or CIDR range, not a hostname — Asterisk rejects
        // a hostname string with "failed field value validation" (confirmed against the real
        // endpoint). SipTrunkConfig.ProviderHost must be the provider's actual signaling IP,
        // matching how trunk providers typically document it (e.g. Twilio publishes static
        // signaling IP ranges) — not a doc-specified constraint, confirm before shipping.
        await this.PutPjsipConfigAsync("identify", config.TrunkName,
        [
            new ConfigTuple { Attribute = "endpoint", Value = config.TrunkName },
            new ConfigTuple { Attribute = "match", Value = config.ProviderHost }
        ]);

        // Stored here, before attempting "endpoint" — the business-rule config §8.4's
        // validation flow needs (RetrieveTrunkConfigAsync/RetrieveSpendStatusAsync callers)
        // doesn't functionally depend on the endpoint PJSIP object existing, and gating it on
        // that would mean the still-open upstream bug below (asterisk/asterisk#1655) blocks
        // config retrieval too, not just endpoint creation. ConfigureTrunkAsync still throws
        // below so callers see the real failure and know the trunk isn't fully configured.
        this.trunkConfigs[config.TrunkName] = config;

        await this.PutPjsipConfigAsync("endpoint", config.TrunkName,
        [
            new ConfigTuple { Attribute = "context", Value = this.trunkOptions.TrunkDialplanContext },
            new ConfigTuple { Attribute = "disallow", Value = "all" },
            new ConfigTuple { Attribute = "allow", Value = "ulaw,alaw" },
            new ConfigTuple { Attribute = "aors", Value = config.TrunkName },
            new ConfigTuple { Attribute = "outbound_auth", Value = config.TrunkName },
            new ConfigTuple { Attribute = "identify_by", Value = "ip,username" }
        ]);
    }

    public ValueTask<SipTrunkConfig> RetrieveTrunkConfigAsync(string trunkName) =>
        this.trunkConfigs.TryGetValue(trunkName, out SipTrunkConfig? config)
            ? ValueTask.FromResult(config)
            : throw new HttpResponseNotFoundException();

    public ValueTask<IReadOnlyList<string>> ListConfiguredTrunkNamesAsync() =>
        ValueTask.FromResult<IReadOnlyList<string>>(this.trunkConfigs.Keys.ToList());

    public async ValueTask RemoveTrunkAsync(string trunkName)
    {
        foreach (string objectType in new[] { "endpoint", "identify", "auth", "aor" })
        {
            string relativeUrl = string.Format(
                PjsipConfigRelativeUrlFormat, objectType, Uri.EscapeDataString(trunkName));

            try
            {
                await this.DeleteAsync(relativeUrl);
            }
            catch (HttpResponseNotFoundException)
            {
                // Idempotent delete — an object that never existed (or was already removed) is
                // not a failure. Also the practical effect of the endpoint object type never
                // being creatable via realtime Postgres in the first place (see
                // ConfigureTrunkAsync's comment) — this keeps cleanup from failing on it.
            }
        }

        this.trunkConfigs.TryRemove(trunkName, out _);
    }

    private async ValueTask PutPjsipConfigAsync(string objectType, string id, IReadOnlyList<ConfigTuple> fields)
    {
        string relativeUrl = string.Format(PjsipConfigRelativeUrlFormat, objectType, Uri.EscapeDataString(id));
        await this.PutAsync(relativeUrl, new PjsipConfigRequestBody { Fields = fields });
    }
}
