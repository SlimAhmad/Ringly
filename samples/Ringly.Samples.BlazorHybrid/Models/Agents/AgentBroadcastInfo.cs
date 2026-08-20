namespace Ringly.Samples.BlazorHybrid.Models.Agents;

// Local shape for Ringly.Samples.WebApi's AgentsController broadcast stream (Ringly.Abstractions.
// Models.CallBroadcastEvent) — same reasoning as Models/Support/SupportRouteResult.cs: this app
// has no project reference to Ringly.Abstractions, so the broker deserializes the SSE payload
// into this app-local model instead.
public sealed class AgentBroadcastInfo
{
    public string ChannelId { get; set; } = string.Empty;
    public string CallerNumber { get; set; } = string.Empty;
    public string CalledExtension { get; set; } = string.Empty;
}
