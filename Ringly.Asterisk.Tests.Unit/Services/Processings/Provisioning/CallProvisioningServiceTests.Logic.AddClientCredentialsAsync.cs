using System.Text.RegularExpressions;
using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Tests.Unit.Services.Processings.Provisioning;

public partial class CallProvisioningServiceTests
{
    [Fact]
    public async Task ShouldAddClientCredentialsAsync()
    {
        // given
        Guid inputClientId = GetRandomId();
        SipEndpointConfig? capturedConfig = null;

        this.sipEndpointConfigFoundationServiceMock.Setup(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()))
                .Callback<SipEndpointConfig>(config => capturedConfig = config)
                .Returns(ValueTask.CompletedTask);

        // when
        SipCredentials actualCredentials =
            await this.callProvisioningService.AddClientCredentialsAsync(inputClientId);

        // then
        actualCredentials.ClientId.Should().Be(inputClientId);
        actualCredentials.Extension.Should().MatchRegex(new Regex("^[0-9]{6}$").ToString());
        actualCredentials.Password.Should().HaveLength(24);

        capturedConfig.Should().NotBeNull();
        capturedConfig!.Extension.Should().Be(actualCredentials.Extension);
        capturedConfig.Password.Should().Be(actualCredentials.Password);

        this.sipEndpointConfigFoundationServiceMock.Verify(service =>
            service.AddSipEndpointConfigAsync(It.IsAny<SipEndpointConfig>()),
                Times.Once);

        this.sipEndpointConfigFoundationServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
