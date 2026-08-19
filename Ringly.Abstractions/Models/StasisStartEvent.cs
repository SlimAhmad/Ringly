namespace Ringly.Abstractions.Models;

public class StasisStartEvent
{
    public string ChannelId { get; set; } = string.Empty;
    public IReadOnlyList<string> Args { get; set; } = [];

    // ARI's real channel JSON carries a "caller": { "number": "...", "name": "..." } object —
    // this is the caller's own extension, distinct from Args[0] (the DIALED/callee extension for
    // a client-dialed call). Null when ARI doesn't report one (e.g. an anonymous/unknown caller).
    public string? CallerExtension { get; set; }
}
