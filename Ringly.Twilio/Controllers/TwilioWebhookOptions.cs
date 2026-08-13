namespace Ringly.Twilio.Controllers;

public class TwilioWebhookOptions
{
    public string AuthToken { get; set; } = string.Empty;

    // Twilio signs the exact public URL it called. If TLS terminates upstream of this app
    // (a reverse proxy/load balancer — very common in production) the request URL ASP.NET Core
    // reconstructs internally can differ from what Twilio actually signed (scheme/host
    // mismatch is the classic case, called out in Twilio's own RequestValidator source as a
    // known gotcha). Set this to the exact public base URL (e.g.
    // "https://api.example.com/webhooks/twilio") to override reconstruction from the request
    // when the two don't match; leave null/empty to use the request URL as seen by the app.
    public string? PublicBaseUrlOverride { get; set; }
}
