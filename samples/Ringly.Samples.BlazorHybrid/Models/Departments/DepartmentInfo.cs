namespace Ringly.Samples.BlazorHybrid.Models.Departments;

// Local shape for Ringly.Samples.WebApi's QueuesController response
// (Ringly.CallCenter.Abstractions.Models.HoldingBridge) — same reasoning as SupportRouteResult:
// this app has no project reference to that server-side library.
public sealed class DepartmentInfo
{
    public string BridgeId { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}
