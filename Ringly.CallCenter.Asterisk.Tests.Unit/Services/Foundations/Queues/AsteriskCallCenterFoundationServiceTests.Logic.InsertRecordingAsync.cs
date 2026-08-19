using FluentAssertions;
using Moq;
using Ringly.Asterisk.Models;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldInsertRecordingAsync()
    {
        // given
        string inputBridgeId = GetRandomString();
        string inputRecordingName = GetRandomString();
        string inputFormat = GetRandomString();
        LiveRecording liveRecording = CreateRandomLiveRecording();

        var expectedRecordingInfo = new RecordingInfo
        {
            Name = liveRecording.Name,
            State = liveRecording.State
        };

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertRecordingAsync(inputBridgeId, inputRecordingName, inputFormat))
                .ReturnsAsync(liveRecording);

        // when
        RecordingInfo actualRecordingInfo = await this.asteriskCallCenterFoundationService.InsertRecordingAsync(
            inputBridgeId, inputRecordingName, inputFormat);

        // then
        actualRecordingInfo.Should().BeEquivalentTo(expectedRecordingInfo);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertRecordingAsync(inputBridgeId, inputRecordingName, inputFormat),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
