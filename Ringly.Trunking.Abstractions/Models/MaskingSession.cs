namespace Ringly.Trunking.Abstractions.Models;

// §8.3 — maps a masked number a driver dials back into an active trip's other party. Not
// doc-specified beyond the properties §8.3's example code directly references
// (OtherPartyExtension, IsExpired) — TripId/CreatedDate/ExpiresAt are a reasonable minimal
// shape for a session-lookup store, confirm before shipping.
public class MaskingSession
{
    public string MaskedNumber { get; set; } = string.Empty;
    public string OtherPartyExtension { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= this.ExpiresAt;
}
