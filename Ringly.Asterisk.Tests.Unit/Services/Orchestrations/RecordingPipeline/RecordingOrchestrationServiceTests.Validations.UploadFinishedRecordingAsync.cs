using FluentAssertions;
using Moq;
using Ringly.Asterisk.Models.Orchestrations.RecordingPipeline.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Orchestrations.RecordingPipeline;

public partial class RecordingOrchestrationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnUploadIfRecordingNameIsInvalidAndLogItAsync(
        string? invalidRecordingName)
    {
        // given
        string someFormat = "wav";
        var invalidRecordingPipelineRequestException = new InvalidRecordingPipelineRequestException();

        invalidRecordingPipelineRequestException.UpsertDataList(
            key: "recordingName",
            value: "Value is required");

        var expectedException =
            new RecordingPipelineValidationException(invalidRecordingPipelineRequestException);

        // when
        ValueTask<Uri> uploadTask = this.recordingOrchestrationService.UploadFinishedRecordingAsync(
            invalidRecordingName!, someFormat);

        RecordingPipelineValidationException actualException =
            await Assert.ThrowsAsync<RecordingPipelineValidationException>(uploadTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.recordingStorageProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnUploadIfFormatIsInvalidAndLogItAsync(string? invalidFormat)
    {
        // given
        string someRecordingName = GetRandomString();
        var invalidRecordingPipelineRequestException = new InvalidRecordingPipelineRequestException();

        invalidRecordingPipelineRequestException.UpsertDataList(
            key: "format",
            value: "Value is required");

        var expectedException =
            new RecordingPipelineValidationException(invalidRecordingPipelineRequestException);

        // when
        ValueTask<Uri> uploadTask = this.recordingOrchestrationService.UploadFinishedRecordingAsync(
            someRecordingName, invalidFormat!);

        RecordingPipelineValidationException actualException =
            await Assert.ThrowsAsync<RecordingPipelineValidationException>(uploadTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.recordingStorageProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
