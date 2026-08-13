namespace Ringly.AiAgent.Abstractions.Models;

public class AiAgentSession
{
    public Guid AiSessionId { get; set; }
    public string ChannelId { get; set; } = string.Empty;
    public DateTimeOffset StartedDate { get; set; }
}
