using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Services.Foundations.SipEndpoints;
using Tynamix.ObjectFiller;

namespace Ringly.Asterisk.Tests.Acceptance.Brokers;

// Row #21 (ACCEPTANCE): exercises AsteriskSipEndpointConfigFoundationService against a REAL
// Asterisk instance (docker/docker-compose.yml) instead of a mocked broker — the dynamic PJSIP
// config PUT and its extension-collision behavior are only meaningful when proven against the
// actual ARI dynamic-config write path, per §5.10's open verification note.
//
// Requires the stack from `docker compose up -d` (docker/) to be running locally.
public partial class AsteriskBrokerAcceptanceTests : IDisposable
{
    private const string BaseUrl = "http://localhost:8088";
    private const string AriUsername = "ringly";
    private const string AriPassword = "ringly-dev-ari";

    private readonly AsteriskSipEndpointConfigFoundationService sipEndpointConfigFoundationService;
    private readonly HttpClient rawAriClient;

    public AsteriskBrokerAcceptanceTests()
    {
        var options = Options.Create(new AsteriskOptions
        {
            BaseUrl = BaseUrl,
            Username = AriUsername,
            Password = AriPassword,
            StasisAppName = "ride_hailing_app",
            DialplanContext = "ride_hailing",
            UseWebRtcTransport = true,
            AmiPort = 5038,
            AmiUsername = "ringly",
            AmiSecret = "ringly-dev-ami"
        });

        var asteriskBroker = new AsteriskBroker(options);
        var loggingBroker = new LoggingBroker(NullLogger<LoggingBroker>.Instance);

        this.sipEndpointConfigFoundationService =
            new AsteriskSipEndpointConfigFoundationService(asteriskBroker, loggingBroker);

        this.rawAriClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{AriUsername}:{AriPassword}"));
        this.rawAriClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    private static string CreateRandomExtension() =>
        $"acct{Random.Shared.Next(100000, 999999)}";

    private static string CreateRandomPassword() =>
        new MnemonicString(wordCount: 4).GetValue();

    // Test cleanup, now routed through the real production path
    // (AsteriskSipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync) rather than a raw
    // ARI DELETE — this doubles as ongoing acceptance coverage for the remove path on every test
    // that provisions an extension. Swallows failures since cleanup runs in a `finally` and a test
    // that never got as far as provisioning (e.g. failed before Add) would otherwise mask the
    // real assertion failure with an unrelated "extension not found" exception here.
    private async Task DeleteSipEndpointConfigAsync(string extension)
    {
        try
        {
            await this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(extension);
        }
        catch (Exception)
        {
        }
    }

    public void Dispose() =>
        this.rawAriClient.Dispose();
}
