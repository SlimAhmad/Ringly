using Moq;
using Ringly.Abstractions.Models;
using RESTFulSense.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationServiceTests
{
    [Fact]
    public async Task ShouldAddSipEndpointConfigAsync()
    {
        // given
        SipEndpointConfig inputConfig = CreateRandomSipEndpointConfig();

        this.asteriskBrokerMock.Setup(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputConfig.Extension))
                .ThrowsAsync(new HttpResponseNotFoundException());

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertSipEndpointConfigAsync(inputConfig))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(inputConfig);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputConfig.Extension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertSipEndpointConfigAsync(inputConfig),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
