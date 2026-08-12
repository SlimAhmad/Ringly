namespace Ringly.Client.SipSorcery;

public class SipSorceryCallOptions
{
    public string RegistrarHost { get; set; } = string.Empty;
    public int RegistrationExpirySeconds { get; set; } = 120;
}
