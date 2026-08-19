using Moq;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldDeleteStoredRecordingAsync()
    {
        // given
        string inputRecordingName = GetRandomString();

        this.asteriskBrokerMock.Setup(broker =>
            broker.DeleteStoredRecordingAsync(inputRecordingName))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.asteriskCallCenterFoundationService.DeleteStoredRecordingAsync(inputRecordingName);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.DeleteStoredRecordingAsync(inputRecordingName),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
