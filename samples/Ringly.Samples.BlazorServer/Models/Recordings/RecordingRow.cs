namespace Ringly.Samples.BlazorServer.Models.Recordings;

// Local shape for Ringly.Samples.WebApi's persisted Recording row (returned by
// GET api/recordings) — same reasoning as other local stand-ins in this app.
public sealed class RecordingRow
{
    public string BridgeId { get; set; } = string.Empty;
    public string RecordingName { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public DateTimeOffset StartedDate { get; set; }
}
