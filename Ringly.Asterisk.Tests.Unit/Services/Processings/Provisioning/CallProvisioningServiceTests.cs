using System.Linq.Expressions;
using Moq;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Services.Foundations.SipEndpoints;
using Ringly.Asterisk.Services.Processings.Provisioning;
using Xeptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Processings.Provisioning;

public partial class CallProvisioningServiceTests
{
    private readonly Mock<IAsteriskSipEndpointConfigFoundationService> sipEndpointConfigFoundationServiceMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly CallProvisioningService callProvisioningService;

    public CallProvisioningServiceTests()
    {
        this.sipEndpointConfigFoundationServiceMock = new Mock<IAsteriskSipEndpointConfigFoundationService>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.callProvisioningService = new CallProvisioningService(
            sipEndpointConfigFoundationService: this.sipEndpointConfigFoundationServiceMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static Guid GetRandomId() =>
        Guid.NewGuid();
}
