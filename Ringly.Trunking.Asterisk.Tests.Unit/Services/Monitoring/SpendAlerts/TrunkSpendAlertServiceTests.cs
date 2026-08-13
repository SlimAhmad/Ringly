using Moq;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Brokers;
using Ringly.Trunking.Asterisk.Services.Monitoring.SpendAlerts;
using Tynamix.ObjectFiller;

namespace Ringly.Trunking.Asterisk.Tests.Unit.Services.Monitoring.SpendAlerts;

public partial class TrunkSpendAlertServiceTests
{
    private readonly Mock<ISipTrunkBroker> sipTrunkBrokerMock;
    private readonly Mock<ITrunkSpendAlertNotifier> alertNotifierMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly TrunkSpendAlertService trunkSpendAlertService;

    public TrunkSpendAlertServiceTests()
    {
        this.sipTrunkBrokerMock = new Mock<ISipTrunkBroker>();
        this.alertNotifierMock = new Mock<ITrunkSpendAlertNotifier>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.trunkSpendAlertService = new TrunkSpendAlertService(
            sipTrunkBroker: this.sipTrunkBrokerMock.Object,
            alertNotifier: this.alertNotifierMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string GetRandomString() =>
        new MnemonicString(wordCount: 2).GetValue().Replace(" ", string.Empty);

    private static SipTrunkConfig CreateRandomSipTrunkConfig(string trunkName) =>
        new()
        {
            TrunkName = trunkName,
            ProviderHost = "203.0.113.5",
            Username = GetRandomString(),
            Password = GetRandomString(),
            MaxConcurrentCallsPerTrunk = 5,
            MaxDailySpendUsd = 100m
        };

    private static TrunkCallLimitStatus CreateWithinLimitsStatus(string trunkName) =>
        new()
        {
            TrunkName = trunkName,
            ActiveCallCount = 0,
            SpendTodayUsd = 0m,
            IsOverLimit = false
        };
}
