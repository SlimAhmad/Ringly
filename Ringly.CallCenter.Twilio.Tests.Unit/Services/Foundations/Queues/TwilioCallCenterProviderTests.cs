using System.Linq.Expressions;
using Moq;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Twilio.Services.Foundations.Queues;
using Ringly.Twilio.Brokers;
using Ringly.Twilio.Models;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.CallCenter.Twilio.Tests.Unit.Services.Foundations.Queues;

public partial class TwilioCallCenterProviderTests
{
    private readonly Mock<ITwilioBroker> twilioBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly TwilioCallCenterProvider twilioCallCenterProvider;

    public TwilioCallCenterProviderTests()
    {
        this.twilioBrokerMock = new Mock<ITwilioBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.twilioCallCenterProvider = new TwilioCallCenterProvider(
            twilioBroker: this.twilioBrokerMock.Object,
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

    private static TwilioTaskQueue CreateRandomTaskQueue() =>
        CreateTaskQueueFiller().Create();

    private static Filler<TwilioTaskQueue> CreateTaskQueueFiller()
    {
        var filler = new Filler<TwilioTaskQueue>();
        filler.Setup();

        return filler;
    }
}
