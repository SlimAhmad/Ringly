using Moq;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldCopyStoredRecordingAsync()
    {
        // given
        string inputRecordingName = GetRandomString();
        string inputDestinationName = GetRandomString();

        this.asteriskBrokerMock.Setup(broker =>
            broker.CopyStoredRecordingAsync(inputRecordingName, inputDestinationName))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.asteriskCallCenterFoundationService.CopyStoredRecordingAsync(
            inputRecordingName, inputDestinationName);

        // then
        this.asteriskBrokerMock.Verify(broker =>
            broker.CopyStoredRecordingAsync(inputRecordingName, inputDestinationName),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
