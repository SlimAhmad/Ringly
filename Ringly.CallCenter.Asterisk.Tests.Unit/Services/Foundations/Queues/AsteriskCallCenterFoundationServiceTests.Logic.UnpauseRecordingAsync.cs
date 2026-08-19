using Moq;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldUnpauseRecordingAsync()
    {
        // given
        string inputRecordingName = GetRandomString();

        this.asteriskBrokerMock.Setup(broker =>
            broker.UnpauseRecordingAsync(inputRecordingName))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.asteriskCallCenterFoundationService.UnpauseRecordingAsync(inputRecordingName);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.UnpauseRecordingAsync(inputRecordingName),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
