using System.Linq.Expressions;
using Moq;
using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Services.Orchestrations.Onboarding;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Orchestrations.Onboarding;

public partial class UserOnboardingOrchestrationServiceTests
{
    private readonly Mock<ICallProvisioningService> callProvisioningServiceMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly UserOnboardingOrchestrationService userOnboardingOrchestrationService;

    public UserOnboardingOrchestrationServiceTests()
    {
        this.callProvisioningServiceMock = new Mock<ICallProvisioningService>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.userOnboardingOrchestrationService = new UserOnboardingOrchestrationService(
            callProvisioningService: this.callProvisioningServiceMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static Guid GetRandomId() =>
        Guid.NewGuid();

    private static SipCredentials CreateRandomSipCredentials() =>
        CreateSipCredentialsFiller().Create();

    private static Filler<SipCredentials> CreateSipCredentialsFiller()
    {
        var filler = new Filler<SipCredentials>();
        filler.Setup();

        return filler;
    }
}
