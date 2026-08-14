namespace Ringly.Client.Abstractions.Models;

public class CallClientEvent
{
    public string EventType { get; set; } = string.Empty;
    public CallHandle Handle { get; set; } = new();
    public DateTimeOffset OccurredDate { get; set; }
    public string RemoteExtension { get; set; } = string.Empty;
}
