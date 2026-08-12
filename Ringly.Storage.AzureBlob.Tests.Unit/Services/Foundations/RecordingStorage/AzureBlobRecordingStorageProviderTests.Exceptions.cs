using Azure;
using FluentAssertions;
using Moq;
using Ringly.Storage.AzureBlob.Models.Foundations.RecordingStorage.Exceptions;

namespace Ringly.Storage.AzureBlob.Tests.Unit.Services.Foundations.RecordingStorage;

public partial class AzureBlobRecordingStorageProviderTests
{
    [Fact]
    public async Task ShouldThrowDependencyExceptionOnDeleteIfRequestFailedErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingId = GetRandomString();
        var requestFailedException = new RequestFailedException(GetRandomString());

        var failedAzureBlobDependencyException =
            new FailedAzureBlobDependencyException(requestFailedException);

        var expectedException =
            new RecordingStorageDependencyException(failedAzureBlobDependencyException);

        this.azureBlobBrokerMock.Setup(broker =>
            broker.DeleteAsync(someRecordingId))
                .ThrowsAsync(requestFailedException);

        // when
        ValueTask deleteTask = this.recordingStorageProvider.DeleteRecordingAsync(someRecordingId);

        RecordingStorageDependencyException actualException =
            await Assert.ThrowsAsync<RecordingStorageDependencyException>(deleteTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.azureBlobBrokerMock.Verify(broker =>
            broker.DeleteAsync(someRecordingId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.azureBlobBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnDeleteIfErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingId = GetRandomString();
        var exception = new Exception();
        var failedRecordingStorageServiceException = new FailedRecordingStorageServiceException(exception);

        var expectedException =
            new RecordingStorageServiceException(failedRecordingStorageServiceException);

        this.azureBlobBrokerMock.Setup(broker =>
            broker.DeleteAsync(someRecordingId))
                .ThrowsAsync(exception);

        // when
        ValueTask deleteTask = this.recordingStorageProvider.DeleteRecordingAsync(someRecordingId);

        RecordingStorageServiceException actualException =
            await Assert.ThrowsAsync<RecordingStorageServiceException>(deleteTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.azureBlobBrokerMock.Verify(broker =>
            broker.DeleteAsync(someRecordingId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.azureBlobBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
