namespace Ringly.Abstractions.Models;

public class ClaimResult
{
    public bool Claimed { get; set; }
    public string ChannelId { get; set; } = string.Empty;
    public string BridgeId { get; set; } = string.Empty;
}
