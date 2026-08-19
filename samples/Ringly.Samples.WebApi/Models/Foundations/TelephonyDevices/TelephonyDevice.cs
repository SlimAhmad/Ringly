namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;

public class TelephonyDevice
{
    public Guid Id { get; set; }
    public Guid IdentityId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastRegisteredAt { get; set; }
    public DateTimeOffset? LastUnregisteredAt { get; set; }
}
