namespace Ringly.Abstractions.Models;

public class TransferParty
{
    public string ChannelId { get; set; } = string.Empty;
    public string BridgeId { get; set; } = string.Empty;
    public string ConnectedChannelId { get; set; } = string.Empty;
    public string RequestedDestination { get; set; } = string.Empty;
}
