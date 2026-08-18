namespace Ringly.Client.SipSorcery;

public class SipSorceryCallOptions
{
    public string RegistrarHost { get; set; } = string.Empty;
    public int RegistrationExpirySeconds { get; set; } = 120;
    public List<string> IceServerUrls { get; set; } = [];
    public string? IceServerUsername { get; set; }
    public string? IceServerCredential { get; set; }

    // Forces RTCConfiguration.X_BindAddress (see SipSorceryCallClient's constructor) — without
    // this, ICE gathers a host candidate on every local network adapter, including Docker
    // Desktop's WSL2 bridge/vEthernet adapters, which commonly land in the same private subnet
    // range as this project's own docker-compose network (172.22.0.0/16) purely by coincidence.
    // Confirmed live: the remote peer nominated exactly that kind of candidate (172.22.0.4, a
    // Windows-host-only virtual adapter, not this machine's real LAN-facing IP) and the DTLS
    // handshake then timed out — nothing on the other device can ever reach a host-only virtual
    // adapter. Set this to the machine's actual LAN IP to gather (and bind RTP sockets to) only
    // that interface.
    public string? BindAddress { get; set; }
}
