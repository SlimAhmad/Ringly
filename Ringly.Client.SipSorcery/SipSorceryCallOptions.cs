namespace Ringly.Client.SipSorcery;

public class SipSorceryCallOptions
{
    public string RegistrarHost { get; set; } = string.Empty;
    public int RegistrationExpirySeconds { get; set; } = 120;
    public List<string> IceServerUrls { get; set; } = [];
    public string? IceServerUsername { get; set; }
    public string? IceServerCredential { get; set; }
}
