namespace Ringly.Abstractions.Models;

public class ChannelTransferEvent
{
    public string ChannelId { get; set; } = string.Empty;
    public bool IsAttended { get; set; }
    public string BridgeId { get; set; } = string.Empty;
    public string HeldChannelId { get; set; } = string.Empty;
    public TransferParty ReferTo { get; set; } = new();
    public TransferParty ReferredBy { get; set; } = new();
}
