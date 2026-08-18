using Moq;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.SipEndpoints;

public partial class AsteriskSipEndpointConfigFoundationServiceTests
{
    [Fact]
    public async Task ShouldRemoveSipEndpointConfigAsync()
    {
        // given
        string inputExtension = GetRandomString();

        this.asteriskBrokerMock.Setup(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension))
                .ReturnsAsync([]);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RemoveSipEndpointConfigObjectAsync(It.IsAny<string>(), inputExtension))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(inputExtension);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("auth", inputExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("aor", inputExtension),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldRemoveSipEndpointConfigAsyncEvenIfEndpointObjectAlreadyAbsent()
    {
        // given — "endpoint" already gone (e.g. it was never created, per the known Insert bug)
        // must not stop auth/aor from being removed.
        string inputExtension = GetRandomString();

        this.asteriskBrokerMock.Setup(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension))
                .ReturnsAsync([]);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension))
                .ThrowsAsync(new RESTFulSense.Exceptions.HttpResponseNotFoundException());

        this.asteriskBrokerMock.Setup(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("auth", inputExtension))
                .Returns(ValueTask.CompletedTask);

        this.asteriskBrokerMock.Setup(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("aor", inputExtension))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(inputExtension);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("endpoint", inputExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("auth", inputExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RemoveSipEndpointConfigObjectAsync("aor", inputExtension),
                Times.Once);

        this.asteriskBrokerMock.Verify(broker =>
            broker.RetrieveSipEndpointConfigAsync(inputExtension),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
