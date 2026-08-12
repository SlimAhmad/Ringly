namespace Ringly.Abstractions.Models;

public class CallBroadcastEvent
{
    public string ChannelId { get; set; } = string.Empty;
    public string CallerNumber { get; set; } = string.Empty;
    public string CalledExtension { get; set; } = string.Empty;
    public Dictionary<string, string> ChannelVars { get; set; } = new();
}
