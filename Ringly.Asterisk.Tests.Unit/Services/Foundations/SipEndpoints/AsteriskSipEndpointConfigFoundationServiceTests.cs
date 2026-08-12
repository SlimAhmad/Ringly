using System.Linq.Expressions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Services.Foundations.SipEndpoints;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationServiceTests
{
    private readonly Mock<IAsteriskBroker> asteriskBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly AsteriskSipEndpointConfigFoundationService sipEndpointConfigFoundationService;

    public AsteriskSipEndpointConfigFoundationServiceTests()
    {
        this.asteriskBrokerMock = new Mock<IAsteriskBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.sipEndpointConfigFoundationService = new AsteriskSipEndpointConfigFoundationService(
            asteriskBroker: this.asteriskBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static string GetRandomString() =>
        new MnemonicString(wordCount: 3).GetValue();

    private static SipEndpointConfig CreateRandomSipEndpointConfig() =>
        CreateSipEndpointConfigFiller().Create();

    private static Filler<SipEndpointConfig> CreateSipEndpointConfigFiller()
    {
        var filler = new Filler<SipEndpointConfig>();
        filler.Setup();

        return filler;
    }
}
