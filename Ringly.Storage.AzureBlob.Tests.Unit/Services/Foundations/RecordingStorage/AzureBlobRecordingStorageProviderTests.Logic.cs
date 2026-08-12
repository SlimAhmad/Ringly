using FluentAssertions;
using Moq;

namespace Ringly.Storage.AzureBlob.Tests.Unit.Services.Foundations.RecordingStorage;

public partial class AzureBlobRecordingStorageProviderTests
{
    [Fact]
    public async Task ShouldUploadRecordingAsync()
    {
        // given
        string inputLocalFilePath = GetRandomString();
        string inputRecordingId = GetRandomString();
        Uri returnedUri = GetRandomUri();

        this.azureBlobBrokerMock.Setup(broker =>
            broker.UploadAsync(inputRecordingId, inputLocalFilePath))
                .ReturnsAsync(returnedUri);

        // when
        Uri actualUri = await this.recordingStorageProvider.UploadRecordingAsync(
            inputLocalFilePath, inputRecordingId);

        // then
        actualUri.Should().Be(returnedUri);

        this.azureBlobBrokerMock.Verify(broker =>
            broker.UploadAsync(inputRecordingId, inputLocalFilePath),
                Times.Once);

        this.azureBlobBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldDownloadRecordingAsync()
    {
        // given
        string inputRecordingId = GetRandomString();
        using var returnedStream = new MemoryStream();

        this.azureBlobBrokerMock.Setup(broker =>
            broker.DownloadAsync(inputRecordingId))
                .ReturnsAsync(returnedStream);

        // when
        Stream actualStream = await this.recordingStorageProvider.DownloadRecordingAsync(inputRecordingId);

        // then
        actualStream.Should().BeSameAs(returnedStream);

        this.azureBlobBrokerMock.Verify(broker =>
            broker.DownloadAsync(inputRecordingId),
                Times.Once);

        this.azureBlobBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldDeleteRecordingAsync()
    {
        // given
        string inputRecordingId = GetRandomString();

        this.azureBlobBrokerMock.Setup(broker =>
            broker.DeleteAsync(inputRecordingId))
                .Returns(ValueTask.CompletedTask);

        // when
        await this.recordingStorageProvider.DeleteRecordingAsync(inputRecordingId);

        // then
        this.azureBlobBrokerMock.Verify(broker =>
            broker.DeleteAsync(inputRecordingId),
                Times.Once);

        this.azureBlobBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldGenerateTemporaryAccessUrlAsync()
    {
        // given
        string inputRecordingId = GetRandomString();
        TimeSpan inputExpiry = GetRandomTimeSpan();
        Uri returnedUri = GetRandomUri();

        this.azureBlobBrokerMock.Setup(broker =>
            broker.GenerateSasUriAsync(inputRecordingId, inputExpiry))
                .ReturnsAsync(returnedUri);

        // when
        Uri actualUri = await this.recordingStorageProvider.GenerateTemporaryAccessUrlAsync(
            inputRecordingId, inputExpiry);

        // then
        actualUri.Should().Be(returnedUri);

        this.azureBlobBrokerMock.Verify(broker =>
            broker.GenerateSasUriAsync(inputRecordingId, inputExpiry),
                Times.Once);

        this.azureBlobBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
