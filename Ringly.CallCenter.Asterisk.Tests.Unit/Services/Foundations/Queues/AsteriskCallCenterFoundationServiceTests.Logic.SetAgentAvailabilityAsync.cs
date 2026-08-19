using Moq;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldSetAgentAvailabilityAsync(bool inputIsAvailable)
    {
        // given
        string inputAgentAppName = GetRandomString();

        this.asteriskBrokerMock.Setup(broker =>
            broker.SetAgentAvailabilityAsync(inputAgentAppName, inputIsAvailable))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.asteriskCallCenterFoundationService.SetAgentAvailabilityAsync(
            inputAgentAppName, inputIsAvailable);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.SetAgentAvailabilityAsync(inputAgentAppName, inputIsAvailable),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
