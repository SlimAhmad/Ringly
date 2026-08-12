using FluentAssertions;
using Moq;

namespace Ringly.Asterisk.Tests.Unit.Services.Orchestrations.RecordingPipeline;

public partial class RecordingOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldUploadFinishedRecordingAsync()
    {
        // given
        string inputRecordingName = GetRandomString();
        string inputFormat = "wav";
        Uri returnedUri = GetRandomUri();

        string expectedLocalFilePath = Path.Combine(
            this.recordingPipelineOptions.RecordingsSpoolPath,
            $"{inputRecordingName}.{inputFormat}");

        this.recordingStorageProviderMock.Setup(provider =>
            provider.UploadRecordingAsync(expectedLocalFilePath, inputRecordingName))
                .ReturnsAsync(returnedUri);

        // when
        Uri actualUri = await this.recordingOrchestrationService.UploadFinishedRecordingAsync(
            inputRecordingName, inputFormat);

        // then
        actualUri.Should().Be(returnedUri);

        this.recordingStorageProviderMock.Verify(provider =>
            provider.UploadRecordingAsync(expectedLocalFilePath, inputRecordingName),
                Times.Once);

        this.recordingStorageProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
