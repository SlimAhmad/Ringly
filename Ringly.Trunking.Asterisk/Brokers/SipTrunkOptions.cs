namespace Ringly.Trunking.Asterisk.Brokers;

public class SipTrunkOptions
{
    // Asterisk ARI connection — same server client provisioning talks to, but this broker keeps
    // its own connection deliberately, matching the doc's "separate concern" design (§8).
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string StasisAppName { get; set; } = string.Empty;

    // §8.5 — PSTN-shaped numbers route through Stasis via this dialplan context so the app
    // decides trunk selection, not the static dialplan.
    public string TrunkDialplanContext { get; set; } = string.Empty;
}
