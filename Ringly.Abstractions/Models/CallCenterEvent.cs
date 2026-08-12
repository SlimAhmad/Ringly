namespace Ringly.Abstractions.Models;

public class CallCenterEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public Guid? CallSessionId { get; set; }
    public string AgentExtension { get; set; } = string.Empty;
    public string RecordingName { get; set; } = string.Empty;
    public string RecordingPath { get; set; } = string.Empty;
    public DateTimeOffset OccurredDate { get; set; }
}
