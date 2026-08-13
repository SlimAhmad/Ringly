using System.Linq.Expressions;
using Moq;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Brokers;
using Ringly.Trunking.Asterisk.Services.Foundations.Trunks;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Trunking.Asterisk.Tests.Unit.Services.Foundations.Trunks;

public partial class SipTrunkFoundationServiceTests
{
    private readonly Mock<ISipTrunkBroker> sipTrunkBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly SipTrunkFoundationService sipTrunkFoundationService;

    public SipTrunkFoundationServiceTests()
    {
        this.sipTrunkBrokerMock = new Mock<ISipTrunkBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.sipTrunkFoundationService = new SipTrunkFoundationService(
            sipTrunkBroker: this.sipTrunkBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static string GetRandomString() =>
        new MnemonicString(wordCount: 3).GetValue();

    private static string CreateRandomTrunkName() =>
        "trunk" + GetRandomString().Replace(" ", string.Empty);

    // Domestic (NANP, +1) — satisfies the default domestic-only destination rule without
    // needing AllowedDestinationCountryCodes/InternationalDialingEnabled configured.
    private static string CreateRandomDomesticPhoneNumber() =>
        $"+1{Random.Shared.Next(200, 999)}{Random.Shared.Next(1000000, 9999999)}";

    private static string CreateRandomInternationalPhoneNumber(string countryCode) =>
        $"+{countryCode}{Random.Shared.Next(100000000, 999999999)}";

    private static SipTrunkConfig CreateRandomSipTrunkConfig() =>
        new()
        {
            TrunkName = CreateRandomTrunkName(),
            ProviderHost = "203.0.113.5",
            Username = GetRandomString(),
            Password = GetRandomString(),
            AllowedDestinationCountryCodes = null,
            InternationalDialingEnabled = false,
            MaxDailySpendUsd = null,
            MaxConcurrentCallsPerTrunk = 5
        };

    private static TrunkCallLimitStatus CreateRandomWithinLimitsStatus(string trunkName) =>
        new()
        {
            TrunkName = trunkName,
            ActiveCallCount = 0,
            SpendTodayUsd = 0m,
            IsOverLimit = false
        };
}
