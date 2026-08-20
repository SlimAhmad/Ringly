using FluentAssertions;
using Moq;
using Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnCopyStoredRecordingIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingName = GetRandomString();
        string someDestinationName = GetRandomString();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidRecordingRequestException = new InvalidRecordingRequestException();
        var expectedException = new RecordingDependencyValidationException(invalidRecordingRequestException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.CopyStoredRecordingAsync(someRecordingName, someDestinationName))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask copyTask = this.asteriskCallCenterFoundationService.CopyStoredRecordingAsync(
            someRecordingName, someDestinationName);

        RecordingDependencyValidationException actualException =
            await Assert.ThrowsAsync<RecordingDependencyValidationException>(copyTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.CopyStoredRecordingAsync(someRecordingName, someDestinationName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnCopyStoredRecordingIfNotFoundErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingName = GetRandomString();
        string someDestinationName = GetRandomString();
        var httpResponseNotFoundException = new HttpResponseNotFoundException();
        var notFoundRecordingException = new NotFoundRecordingException(httpResponseNotFoundException);
        var expectedException = new RecordingDependencyValidationException(notFoundRecordingException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.CopyStoredRecordingAsync(someRecordingName, someDestinationName))
                .ThrowsAsync(httpResponseNotFoundException);

        // when
        ValueTask copyTask = this.asteriskCallCenterFoundationService.CopyStoredRecordingAsync(
            someRecordingName, someDestinationName);

        RecordingDependencyValidationException actualException =
            await Assert.ThrowsAsync<RecordingDependencyValidationException>(copyTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.CopyStoredRecordingAsync(someRecordingName, someDestinationName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(RecordingCriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnCopyStoredRecordingIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someRecordingName = GetRandomString();
        string someDestinationName = GetRandomString();

        var failedAsteriskRecordingDependencyException =
            new FailedAsteriskRecordingDependencyException(dependencyException);

        var expectedException = new RecordingDependencyException(failedAsteriskRecordingDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.CopyStoredRecordingAsync(someRecordingName, someDestinationName))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask copyTask = this.asteriskCallCenterFoundationService.CopyStoredRecordingAsync(
            someRecordingName, someDestinationName);

        RecordingDependencyException actualException =
            await Assert.ThrowsAsync<RecordingDependencyException>(copyTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.CopyStoredRecordingAsync(someRecordingName, someDestinationName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(RecordingNonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnCopyStoredRecordingIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someRecordingName = GetRandomString();
        string someDestinationName = GetRandomString();

        var failedAsteriskRecordingDependencyException =
            new FailedAsteriskRecordingDependencyException(dependencyException);

        var expectedException = new RecordingDependencyException(failedAsteriskRecordingDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.CopyStoredRecordingAsync(someRecordingName, someDestinationName))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask copyTask = this.asteriskCallCenterFoundationService.CopyStoredRecordingAsync(
            someRecordingName, someDestinationName);

        RecordingDependencyException actualException =
            await Assert.ThrowsAsync<RecordingDependencyException>(copyTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.CopyStoredRecordingAsync(someRecordingName, someDestinationName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnCopyStoredRecordingIfErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingName = GetRandomString();
        string someDestinationName = GetRandomString();
        var exception = new Exception();
        var failedRecordingServiceException = new FailedRecordingServiceException(exception);
        var expectedException = new RecordingServiceException(failedRecordingServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.CopyStoredRecordingAsync(someRecordingName, someDestinationName))
                .ThrowsAsync(exception);

        // when
        ValueTask copyTask = this.asteriskCallCenterFoundationService.CopyStoredRecordingAsync(
            someRecordingName, someDestinationName);

        RecordingServiceException actualException =
            await Assert.ThrowsAsync<RecordingServiceException>(copyTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.CopyStoredRecordingAsync(someRecordingName, someDestinationName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
