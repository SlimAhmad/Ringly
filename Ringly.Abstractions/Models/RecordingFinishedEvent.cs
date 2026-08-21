namespace Ringly.Abstractions.Models;

public class RecordingFinishedEvent
{
    public string RecordingName { get; set; } = string.Empty;

    // ARI's own LiveRecording.state values once a recording ends: "done" (audio file is real and
    // complete), "failed", or "canceled" (no usable audio in either of the latter two).
    public string State { get; set; } = string.Empty;
}
