using FluentAssertions;
using Moq;
using Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnCancelRecordingIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingName = GetRandomString();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidRecordingRequestException = new InvalidRecordingRequestException();
        var expectedException = new RecordingDependencyValidationException(invalidRecordingRequestException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.CancelRecordingAsync(someRecordingName))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask cancelTask = this.asteriskCallCenterFoundationService.CancelRecordingAsync(someRecordingName);

        RecordingDependencyValidationException actualException =
            await Assert.ThrowsAsync<RecordingDependencyValidationException>(cancelTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.CancelRecordingAsync(someRecordingName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnCancelRecordingIfNotFoundErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingName = GetRandomString();
        var httpResponseNotFoundException = new HttpResponseNotFoundException();
        var notFoundRecordingException = new NotFoundRecordingException(httpResponseNotFoundException);
        var expectedException = new RecordingDependencyValidationException(notFoundRecordingException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.CancelRecordingAsync(someRecordingName))
                .ThrowsAsync(httpResponseNotFoundException);

        // when
        ValueTask cancelTask = this.asteriskCallCenterFoundationService.CancelRecordingAsync(someRecordingName);

        RecordingDependencyValidationException actualException =
            await Assert.ThrowsAsync<RecordingDependencyValidationException>(cancelTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.CancelRecordingAsync(someRecordingName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(RecordingCriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnCancelRecordingIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someRecordingName = GetRandomString();

        var failedAsteriskRecordingDependencyException =
            new FailedAsteriskRecordingDependencyException(dependencyException);

        var expectedException = new RecordingDependencyException(failedAsteriskRecordingDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.CancelRecordingAsync(someRecordingName))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask cancelTask = this.asteriskCallCenterFoundationService.CancelRecordingAsync(someRecordingName);

        RecordingDependencyException actualException =
            await Assert.ThrowsAsync<RecordingDependencyException>(cancelTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.CancelRecordingAsync(someRecordingName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(RecordingNonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnCancelRecordingIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someRecordingName = GetRandomString();

        var failedAsteriskRecordingDependencyException =
            new FailedAsteriskRecordingDependencyException(dependencyException);

        var expectedException = new RecordingDependencyException(failedAsteriskRecordingDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.CancelRecordingAsync(someRecordingName))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask cancelTask = this.asteriskCallCenterFoundationService.CancelRecordingAsync(someRecordingName);

        RecordingDependencyException actualException =
            await Assert.ThrowsAsync<RecordingDependencyException>(cancelTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.CancelRecordingAsync(someRecordingName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnCancelRecordingIfErrorOccursAndLogItAsync()
    {
        // given
        string someRecordingName = GetRandomString();
        var exception = new Exception();
        var failedRecordingServiceException = new FailedRecordingServiceException(exception);
        var expectedException = new RecordingServiceException(failedRecordingServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.CancelRecordingAsync(someRecordingName))
                .ThrowsAsync(exception);

        // when
        ValueTask cancelTask = this.asteriskCallCenterFoundationService.CancelRecordingAsync(someRecordingName);

        RecordingServiceException actualException =
            await Assert.ThrowsAsync<RecordingServiceException>(cancelTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.CancelRecordingAsync(someRecordingName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
