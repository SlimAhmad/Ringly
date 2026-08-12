namespace Ringly.Abstractions.Models;

public class CallSession
{
    public Guid CallSessionId { get; set; }
    public string BridgeId { get; set; } = string.Empty;
    public Guid TripId { get; set; }
}
