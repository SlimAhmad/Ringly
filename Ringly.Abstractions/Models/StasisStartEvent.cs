namespace Ringly.Abstractions.Models;

public class StasisStartEvent
{
    public string ChannelId { get; set; } = string.Empty;
    public IReadOnlyList<string> Args { get; set; } = [];
}
