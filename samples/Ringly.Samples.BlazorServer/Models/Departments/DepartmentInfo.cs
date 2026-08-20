namespace Ringly.Samples.BlazorServer.Models.Departments;

// Local shape for Ringly.Samples.WebApi's QueuesController response
// (Ringly.CallCenter.Abstractions.Models.HoldingBridge) — same reasoning as SupportRouteResult:
// this app deliberately keeps its own small local models rather than referencing the server's
// library types directly.
public sealed class DepartmentInfo
{
    public string BridgeId { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}
