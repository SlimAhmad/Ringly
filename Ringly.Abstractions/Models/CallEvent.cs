namespace Ringly.Abstractions.Models;

public class CallEvent
{
    public string EventType { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public Guid? CallSessionId { get; set; }
    public DateTimeOffset OccurredDate { get; set; }
}
