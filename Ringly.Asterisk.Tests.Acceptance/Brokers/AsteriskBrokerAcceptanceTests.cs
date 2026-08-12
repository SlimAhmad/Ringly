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

    // Test-only cleanup — production code has no delete path yet (row #10's
    // RemoveClientCredentialsAsync is deferred), so this goes straight at the real ARI
    // dynamic-config DELETE endpoint rather than through the broker under test.
    private async Task DeleteSipEndpointConfigAsync(string extension)
    {
        foreach (string objectType in new[] { "endpoint", "auth", "aor" })
        {
            await this.rawAriClient.DeleteAsync($"ari/asterisk/config/dynamic/res_pjsip/{objectType}/{extension}");
        }
    }

    public void Dispose() =>
        this.rawAriClient.Dispose();
}
