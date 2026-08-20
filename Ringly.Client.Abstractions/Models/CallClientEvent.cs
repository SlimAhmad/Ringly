namespace Ringly.Client.Abstractions.Models;

public class CallClientEvent
{
    public string EventType { get; set; } = string.Empty;
    public CallHandle Handle { get; set; } = new();
    public DateTimeOffset OccurredDate { get; set; }
    public string RemoteExtension { get; set; } = string.Empty;

    // Only ever populated on "IncomingCall" (parsed from the offer's own SDP) — other event
    // types default to false and callers should ignore this field for them.
    public bool IncludesVideo { get; set; }
}
