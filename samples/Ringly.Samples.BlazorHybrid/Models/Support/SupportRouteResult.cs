namespace Ringly.Samples.BlazorHybrid.Models.Support;

// Local shape for Ringly.Samples.WebApi's SupportController response (Ringly.Abstractions.Models.
// CallSession) — this app has no project reference to Ringly.Abstractions (a server-side library
// this MAUI client shouldn't depend on), so the broker deserializes the same JSON shape into this
// smaller, app-local model instead. Only the field this app's UI actually shows is kept.
public sealed class SupportRouteResult
{
    public string BridgeId { get; set; } = string.Empty;
}
