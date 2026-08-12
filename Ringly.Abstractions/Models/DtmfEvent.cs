namespace Ringly.Abstractions.Models;

public class DtmfEvent
{
    public string ChannelId { get; set; } = string.Empty;
    public string Digit { get; set; } = string.Empty;
    public int DurationMs { get; set; }
}
