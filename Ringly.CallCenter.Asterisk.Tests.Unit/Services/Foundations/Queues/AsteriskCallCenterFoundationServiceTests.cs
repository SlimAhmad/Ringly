using System.Linq.Expressions;
using Moq;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Models;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Services.Foundations.Queues;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    private readonly Mock<IAsteriskBroker> asteriskBrokerMock;
    private readonly Mock<IQueueRegistry> queueRegistryMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly AsteriskCallCenterFoundationService asteriskCallCenterFoundationService;

    public AsteriskCallCenterFoundationServiceTests()
    {
        this.asteriskBrokerMock = new Mock<IAsteriskBroker>();
        this.queueRegistryMock = new Mock<IQueueRegistry>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.asteriskCallCenterFoundationService = new AsteriskCallCenterFoundationService(
            asteriskBroker: this.asteriskBrokerMock.Object,
            queueRegistry: this.queueRegistryMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static string GetRandomString() =>
        new MnemonicString(wordCount: 3).GetValue();

    private static QueueConfig CreateRandomQueueConfig() =>
        CreateQueueConfigFiller().Create();

    private static Filler<QueueConfig> CreateQueueConfigFiller()
    {
        var filler = new Filler<QueueConfig>();
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
}
