namespace Ringly.CallCenter.Abstractions.Models;

// Abstraction-level shape for a recording — Ringly.CallCenter.Abstractions has no project
// reference to Ringly.Asterisk (a concrete broker implementation), so this stands in for
// Ringly.Asterisk.Models.LiveRecording, which the foundation service maps into on the way out
// (the-standard-architecture's "map non-local models" rule for foundation services).
public class RecordingInfo
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
