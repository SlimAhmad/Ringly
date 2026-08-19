using FluentAssertions;
using Moq;
using Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnDeleteStoredRecordingIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingName = GetRandomString();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidRecordingRequestException = new InvalidRecordingRequestException();
        var expectedException = new RecordingDependencyValidationException(invalidRecordingRequestException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.DeleteStoredRecordingAsync(someRecordingName))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask deleteTask =
            this.asteriskCallCenterFoundationService.DeleteStoredRecordingAsync(someRecordingName);

        RecordingDependencyValidationException actualException =
            await Assert.ThrowsAsync<RecordingDependencyValidationException>(deleteTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.DeleteStoredRecordingAsync(someRecordingName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(RecordingCriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnDeleteStoredRecordingIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someRecordingName = GetRandomString();

        var failedAsteriskRecordingDependencyException =
            new FailedAsteriskRecordingDependencyException(dependencyException);

        var expectedException = new RecordingDependencyException(failedAsteriskRecordingDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.DeleteStoredRecordingAsync(someRecordingName))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask deleteTask =
            this.asteriskCallCenterFoundationService.DeleteStoredRecordingAsync(someRecordingName);

        RecordingDependencyException actualException =
            await Assert.ThrowsAsync<RecordingDependencyException>(deleteTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.DeleteStoredRecordingAsync(someRecordingName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(RecordingNonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnDeleteStoredRecordingIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someRecordingName = GetRandomString();

        var failedAsteriskRecordingDependencyException =
            new FailedAsteriskRecordingDependencyException(dependencyException);

        var expectedException = new RecordingDependencyException(failedAsteriskRecordingDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.DeleteStoredRecordingAsync(someRecordingName))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask deleteTask =
            this.asteriskCallCenterFoundationService.DeleteStoredRecordingAsync(someRecordingName);

        RecordingDependencyException actualException =
            await Assert.ThrowsAsync<RecordingDependencyException>(deleteTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.DeleteStoredRecordingAsync(someRecordingName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnDeleteStoredRecordingIfErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingName = GetRandomString();
        var exception = new Exception();
        var failedRecordingServiceException = new FailedRecordingServiceException(exception);
        var expectedException = new RecordingServiceException(failedRecordingServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.DeleteStoredRecordingAsync(someRecordingName))
                .ThrowsAsync(exception);

        // when
        ValueTask deleteTask =
            this.asteriskCallCenterFoundationService.DeleteStoredRecordingAsync(someRecordingName);

        RecordingServiceException actualException =
            await Assert.ThrowsAsync<RecordingServiceException>(deleteTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.DeleteStoredRecordingAsync(someRecordingName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
