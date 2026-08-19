using Moq;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldStopRecordingAsync()
    {
        // given
        string inputRecordingName = GetRandomString();

        this.asteriskBrokerMock.Setup(broker =>
            broker.StopRecordingAsync(inputRecordingName))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.asteriskCallCenterFoundationService.StopRecordingAsync(inputRecordingName);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.StopRecordingAsync(inputRecordingName),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
