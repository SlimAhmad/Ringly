namespace Ringly.Abstractions.Models;

public class ChannelStateChangeEvent
{
    public string ChannelId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
