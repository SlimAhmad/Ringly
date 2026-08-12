using FluentAssertions;
using Moq;
using Ringly.Storage.AzureBlob.Models.Foundations.RecordingStorage.Exceptions;

namespace Ringly.Storage.AzureBlob.Tests.Unit.Services.Foundations.RecordingStorage;

public partial class AzureBlobRecordingStorageProviderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnDownloadIfRecordingIdIsInvalidAndLogItAsync(
        string? invalidRecordingId)
    {
        // given
        var invalidRecordingStorageRequestException = new InvalidRecordingStorageRequestException();

        invalidRecordingStorageRequestException.UpsertDataList(
            key: "recordingId",
            value: "Value is required");

        var expectedException =
            new RecordingStorageValidationException(invalidRecordingStorageRequestException);

        // when
        ValueTask<Stream> downloadTask =
            this.recordingStorageProvider.DownloadRecordingAsync(invalidRecordingId!);

        RecordingStorageValidationException actualException =
            await Assert.ThrowsAsync<RecordingStorageValidationException>(downloadTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.azureBlobBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnUploadIfLocalFilePathIsInvalidAndLogItAsync(
        string? invalidLocalFilePath)
    {
        // given
        string someRecordingId = GetRandomString();
        var invalidRecordingStorageRequestException = new InvalidRecordingStorageRequestException();

        invalidRecordingStorageRequestException.UpsertDataList(
            key: "localFilePath",
            value: "Value is required");

        var expectedException =
            new RecordingStorageValidationException(invalidRecordingStorageRequestException);

        // when
        ValueTask<Uri> uploadTask =
            this.recordingStorageProvider.UploadRecordingAsync(invalidLocalFilePath!, someRecordingId);

        RecordingStorageValidationException actualException =
            await Assert.ThrowsAsync<RecordingStorageValidationException>(uploadTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.azureBlobBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
