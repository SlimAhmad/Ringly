using Moq;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldCancelRecordingAsync()
    {
        // given
        string inputRecordingName = GetRandomString();

        this.asteriskBrokerMock.Setup(broker =>
            broker.CancelRecordingAsync(inputRecordingName))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.asteriskCallCenterFoundationService.CancelRecordingAsync(inputRecordingName);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.CancelRecordingAsync(inputRecordingName),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
