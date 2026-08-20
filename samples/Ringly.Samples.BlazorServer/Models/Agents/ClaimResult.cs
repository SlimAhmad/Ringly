namespace Ringly.Samples.BlazorServer.Models.Agents;

// Local shape for Ringly.Samples.WebApi's AgentsController claim response
// (Ringly.Abstractions.Models.ClaimResult) — same reasoning as other local stand-ins in this app.
public sealed class ClaimResult
{
    public bool Claimed { get; set; }
    public string ChannelId { get; set; } = string.Empty;
    public string BridgeId { get; set; } = string.Empty;
}
