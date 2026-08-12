using FluentAssertions;
using Moq;
using Ringly.Asterisk.Models.Orchestrations.RecordingPipeline.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Orchestrations.RecordingPipeline;

public partial class RecordingOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyExceptionOnUploadIfStorageProviderErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingName = GetRandomString();
        string someFormat = "wav";

        string expectedLocalFilePath = Path.Combine(
            this.recordingPipelineOptions.RecordingsSpoolPath,
            $"{someRecordingName}.{someFormat}");

        var exception = new Exception();
        var failedRecordingPipelineDependencyException = new FailedRecordingPipelineDependencyException(exception);

        var expectedException =
            new RecordingPipelineDependencyException(failedRecordingPipelineDependencyException);

        this.recordingStorageProviderMock.Setup(provider =>
            provider.UploadRecordingAsync(expectedLocalFilePath, someRecordingName))
                .ThrowsAsync(exception);

        // when
        ValueTask<Uri> uploadTask =
            this.recordingOrchestrationService.UploadFinishedRecordingAsync(someRecordingName, someFormat);

        RecordingPipelineDependencyException actualException =
            await Assert.ThrowsAsync<RecordingPipelineDependencyException>(uploadTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.recordingStorageProviderMock.Verify(provider =>
            provider.UploadRecordingAsync(expectedLocalFilePath, someRecordingName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.recordingStorageProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
