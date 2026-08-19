namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

public class TelephonyIdentity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SipUsername { get; set; } = string.Empty;
    public string SipCredential { get; set; } = string.Empty;
    public TelephonyIdentityType Type { get; set; }
    public TelephonyIdentityStatus Status { get; set; }
}
