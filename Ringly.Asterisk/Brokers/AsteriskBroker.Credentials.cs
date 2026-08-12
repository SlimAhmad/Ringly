using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string PjsipConfigRelativeUrlFormat = "asterisk/config/dynamic/pjsip/{0}/{1}";
    private const string TransferHandlingSetVar = "PJSIP_TRANSFER_HANDLING()=ari-only";

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

        await this.PutPjsipConfigAsync("endpoint", config.Extension,
        [
            new ConfigTuple { Attribute = "context", Value = this.asteriskOptions.DialplanContext },
            new ConfigTuple { Attribute = "auth", Value = config.Extension },
            new ConfigTuple { Attribute = "aors", Value = config.Extension },
            new ConfigTuple { Attribute = "webrtc", Value = this.asteriskOptions.UseWebRtcTransport ? "yes" : "no" },
            new ConfigTuple { Attribute = "set_var", Value = TransferHandlingSetVar }
        ]);
    }

    private async ValueTask PutPjsipConfigAsync(string objectType, string id, IReadOnlyList<ConfigTuple> fields)
    {
        string relativeUrl = string.Format(PjsipConfigRelativeUrlFormat, objectType, Uri.EscapeDataString(id));
        await this.PutAsync(relativeUrl, fields);
    }
}
