namespace Ringly.Abstractions.Models;

public class AgentConnection
{
    public string AgentChannelId { get; set; } = string.Empty;

    // The bridge actually carrying two-way audio between agent and customer — Asterisk creates a
    // fresh mixing bridge per connect (ConnectAgentToQueueAsync's own comment explains why: the
    // queue's holding bridge doesn't mix audio), so this is NOT always the same bridgeId the
    // caller passed in. Twilio reuses the same conference throughout, so its BridgeId here just
    // echoes the one it was given.
    public string BridgeId { get; set; } = string.Empty;
}
