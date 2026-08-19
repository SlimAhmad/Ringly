using Moq;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldPauseRecordingAsync()
    {
        // given
        string inputRecordingName = GetRandomString();

        this.asteriskBrokerMock.Setup(broker =>
            broker.PauseRecordingAsync(inputRecordingName))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.asteriskCallCenterFoundationService.PauseRecordingAsync(inputRecordingName);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.PauseRecordingAsync(inputRecordingName),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
