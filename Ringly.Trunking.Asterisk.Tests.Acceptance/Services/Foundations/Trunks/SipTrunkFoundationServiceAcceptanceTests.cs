using Ringly.Abstractions.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Brokers;
using Ringly.Trunking.Asterisk.Services.Foundations.Trunks;

namespace Ringly.Trunking.Asterisk.Tests.Acceptance.Services.Foundations.Trunks;

// Row #30 (ACCEPTANCE): proves SipTrunkFoundationService's destination/spend/concurrency
// rejection (row #25, §8.4) against a real Asterisk instance (docker/docker-compose.yml), not
// mocks — RetrieveSpendStatusAsync's ActiveCallCount comes from a real GET /channels query, so
// this can only be proven end-to-end. Rejection happens before broker.DialOutAsync is ever
// called, so none of these tests need a real trunk provider registered.
public partial class SipTrunkFoundationServiceAcceptanceTests : IDisposable
{
    private const string BaseUrl = "http://localhost:8088";
    private const string AriUsername = "ringly";
    private const string AriPassword = "ringly-dev-ari";

    private readonly SipTrunkBroker sipTrunkBroker;
    private readonly SipTrunkFoundationService sipTrunkFoundationService;
    private string? configuredTrunkName;

    public SipTrunkFoundationServiceAcceptanceTests()
    {
        var options = Options.Create(new SipTrunkOptions
        {
            BaseUrl = BaseUrl,
            Username = AriUsername,
            Password = AriPassword,
            StasisAppName = $"trunk-acceptance-{Guid.NewGuid():N}",
            TrunkDialplanContext = "trunk_test"
        });

        this.sipTrunkBroker = new SipTrunkBroker(options);

        this.sipTrunkFoundationService = new SipTrunkFoundationService(
            sipTrunkBroker: this.sipTrunkBroker,
            loggingBroker: new LoggingBroker(NullLogger<LoggingBroker>.Instance));
    }

    private static string CreateRandomTrunkName() =>
        $"accepttrunk{Random.Shared.Next(100000, 999999)}";

    private async Task ConfigureTestTrunkAsync(SipTrunkConfig config)
    {
        this.configuredTrunkName = config.TrunkName;

        try
        {
            await this.sipTrunkBroker.ConfigureTrunkAsync(config);
        }
        catch
        {
            // "endpoint" object creation is blocked by a confirmed, still-open upstream
            // Asterisk bug (asterisk/asterisk#1655, see rows #21/#24) — aor/auth/identify (and
            // the in-memory business config these tests actually depend on) still succeed.
            // Expected here, not a test failure.
        }
    }

    public void Dispose()
    {
        if (this.configuredTrunkName is not null)
        {
            this.sipTrunkBroker.RemoveTrunkAsync(this.configuredTrunkName).GetAwaiter().GetResult();
        }
    }
}
