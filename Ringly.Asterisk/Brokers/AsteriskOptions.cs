namespace Ringly.Asterisk.Brokers;

public class AsteriskOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string StasisAppName { get; set; } = string.Empty;
    public string DialplanContext { get; set; } = string.Empty;
    public bool UseWebRtcTransport { get; set; } = true;
}
