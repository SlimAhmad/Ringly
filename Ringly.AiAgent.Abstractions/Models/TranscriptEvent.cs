namespace Ringly.AiAgent.Abstractions.Models;

public class TranscriptEvent
{
    public Guid AiSessionId { get; set; }

    // "Caller" | "Agent"
    public string Speaker { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
    public DateTimeOffset OccurredDate { get; set; }
}
