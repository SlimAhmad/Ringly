using Moq;

namespace Ringly.Asterisk.Tests.Unit.Services.Processings.Provisioning;

public partial class CallProvisioningServiceTests
{
    [Fact]
    public async Task ShouldRemoveClientCredentialsAsync()
    {
        // given
        string inputExtension = GetRandomExtension();

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.callProvisioningService.RemoveClientCredentialsAsync(inputExtension);

        // then
        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.RemoveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
