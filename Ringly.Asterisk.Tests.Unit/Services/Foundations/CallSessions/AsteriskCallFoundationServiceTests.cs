using System.Linq.Expressions;
using Moq;
using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Models;
using Ringly.Asterisk.Services.Foundations.CallSessions;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    private readonly Mock<IAsteriskBroker> asteriskBrokerMock;
    private readonly Mock<ISipCredentialsStore> sipCredentialsStoreMock;
    private readonly Mock<IQueueRegistry> queueRegistryMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly AsteriskCallFoundationService callFoundationService;

    public AsteriskCallFoundationServiceTests()
    {
        this.asteriskBrokerMock = new Mock<IAsteriskBroker>();
        this.sipCredentialsStoreMock = new Mock<ISipCredentialsStore>();
        this.queueRegistryMock = new Mock<IQueueRegistry>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.callFoundationService = new AsteriskCallFoundationService(
            asteriskBroker: this.asteriskBrokerMock.Object,
            sipCredentialsStore: this.sipCredentialsStoreMock.Object,
            queueRegistry: this.queueRegistryMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static string GetRandomString() =>
        new MnemonicString(wordCount: 3).GetValue();

    private static CallParticipant CreateRandomCallParticipant() =>
        CreateCallParticipantFiller().Create();

    private static Filler<CallParticipant> CreateCallParticipantFiller()
    {
        var filler = new Filler<CallParticipant>();
        filler.Setup();

        return filler;
    }

    private static Bridge CreateRandomBridge() =>
        CreateBridgeFiller().Create();

    private static Filler<Bridge> CreateBridgeFiller()
    {
        var filler = new Filler<Bridge>();
        filler.Setup();

        return filler;
    }

    private static Channel CreateRandomChannel() =>
        CreateChannelFiller().Create();

    private static Filler<Channel> CreateChannelFiller()
    {
        var filler = new Filler<Channel>();
        filler.Setup();

        return filler;
    }

    private static SipCredentials CreateRandomSipCredentials() =>
        CreateSipCredentialsFiller().Create();

    private static Filler<SipCredentials> CreateSipCredentialsFiller()
    {
        var filler = new Filler<SipCredentials>();
        filler.Setup();

        return filler;
    }

    private static HoldingBridge CreateRandomHoldingBridge() =>
        CreateHoldingBridgeFiller().Create();

    private static Filler<HoldingBridge> CreateHoldingBridgeFiller()
    {
        var filler = new Filler<HoldingBridge>();
        filler.Setup();

        return filler;
    }
}
