namespace Ringly.Samples.WebApi.Models.Foundations.Recordings;

public class Recording
{
    public Guid Id { get; set; }
    public string BridgeId { get; set; } = string.Empty;
    public string RecordingName { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    // Empty until PostStopAsync finishes uploading the finalized file to blob storage.
    public string BlobUrl { get; set; } = string.Empty;

    public DateTimeOffset StartedDate { get; set; }
}
