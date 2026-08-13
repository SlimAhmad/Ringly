using System.Linq.Expressions;
using Moq;
using Ringly.Abstractions;
using Ringly.Trunking.Abstractions;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Brokers;
using Ringly.Trunking.Asterisk.Services.Orchestrations.MaskedCalls;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Trunking.Asterisk.Tests.Unit.Services.Orchestrations.MaskedCalls;

public partial class MaskedCallOrchestrationServiceTests
{
    private readonly Mock<IMaskingSessionStore> maskingSessionStoreMock;
    private readonly Mock<ICallProvider> callProviderMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly MaskedCallOrchestrationService maskedCallOrchestrationService;

    public MaskedCallOrchestrationServiceTests()
    {
        this.maskingSessionStoreMock = new Mock<IMaskingSessionStore>();
        this.callProviderMock = new Mock<ICallProvider>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.maskedCallOrchestrationService = new MaskedCallOrchestrationService(
            maskingSessionStore: this.maskingSessionStoreMock.Object,
            callProvider: this.callProviderMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static string GetRandomString() =>
        new MnemonicString(wordCount: 3).GetValue();

    private static TrunkCallEvent CreateRandomTrunkCallEvent() =>
        new()
        {
            TrunkName = GetRandomString(),
            CallerNumber = "+15555550100",
            DialedNumber = "+15555550199",
            ChannelId = GetRandomString()
        };

    private static MaskingSession CreateRandomActiveMaskingSession(string maskedNumber) =>
        new()
        {
            MaskedNumber = maskedNumber,
            OtherPartyExtension = GetRandomString(),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };

    private static MaskingSession CreateRandomExpiredMaskingSession(string maskedNumber) =>
        new()
        {
            MaskedNumber = maskedNumber,
            OtherPartyExtension = GetRandomString(),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
}
