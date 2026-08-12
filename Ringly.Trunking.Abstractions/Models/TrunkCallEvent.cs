namespace Ringly.Trunking.Abstractions.Models;

public class TrunkCallEvent
{
    public string TrunkName { get; set; } = string.Empty;

    // the real phone number calling in
    public string CallerNumber { get; set; } = string.Empty;

    // the masked number that was dialed
    public string DialedNumber { get; set; } = string.Empty;

    public string ChannelId { get; set; } = string.Empty;
}
