namespace Ringly.Samples.BlazorServer.Models.Agents;

// Local shape for Ringly.Samples.WebApi's AgentsController broadcast stream (Ringly.Abstractions.
// Models.CallBroadcastEvent) — same reasoning as Models/Support/SupportRouteResult.cs.
public sealed class AgentBroadcastInfo
{
    public string ChannelId { get; set; } = string.Empty;
    public string CallerNumber { get; set; } = string.Empty;
    public string CalledExtension { get; set; } = string.Empty;
}
