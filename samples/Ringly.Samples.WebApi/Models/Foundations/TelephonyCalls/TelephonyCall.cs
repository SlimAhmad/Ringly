namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;

public class TelephonyCall
{
    public Guid Id { get; set; }
    public Guid CallerIdentityId { get; set; }
    public Guid RecipientIdentityId { get; set; }
    public TelephonyCallStatus Status { get; set; }
    public string? AsteriskChannelId { get; set; }
    public string? AsteriskBridgeId { get; set; }
    public Guid? TripId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}
