using FluentAssertions;
using Moq;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnInsertRecordingIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someRecordingName = GetRandomString();
        string someFormat = GetRandomString();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidRecordingRequestException = new InvalidRecordingRequestException();
        var expectedException = new RecordingDependencyValidationException(invalidRecordingRequestException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertRecordingAsync(someBridgeId, someRecordingName, someFormat))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<RecordingInfo> insertTask = this.asteriskCallCenterFoundationService.InsertRecordingAsync(
            someBridgeId, someRecordingName, someFormat);

        RecordingDependencyValidationException actualException =
            await Assert.ThrowsAsync<RecordingDependencyValidationException>(insertTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertRecordingAsync(someBridgeId, someRecordingName, someFormat),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> RecordingCriticalDependencyExceptions() =>
    [
        new HttpResponseUnauthorizedException(),
        new HttpResponseForbiddenException(),
        new HttpRequestException()
    ];

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnInsertRecordingIfNotFoundErrorOccursAndLogItAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someRecordingName = GetRandomString();
        string someFormat = GetRandomString();
        var httpResponseNotFoundException = new HttpResponseNotFoundException();
        var notFoundRecordingException = new NotFoundRecordingException(httpResponseNotFoundException);
        var expectedException = new RecordingDependencyValidationException(notFoundRecordingException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertRecordingAsync(someBridgeId, someRecordingName, someFormat))
                .ThrowsAsync(httpResponseNotFoundException);

        // when
        ValueTask<RecordingInfo> insertTask = this.asteriskCallCenterFoundationService.InsertRecordingAsync(
            someBridgeId, someRecordingName, someFormat);

        RecordingDependencyValidationException actualException =
            await Assert.ThrowsAsync<RecordingDependencyValidationException>(insertTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertRecordingAsync(someBridgeId, someRecordingName, someFormat),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(RecordingCriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnInsertRecordingIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someBridgeId = GetRandomString();
        string someRecordingName = GetRandomString();
        string someFormat = GetRandomString();

        var failedAsteriskRecordingDependencyException =
            new FailedAsteriskRecordingDependencyException(dependencyException);

        var expectedException = new RecordingDependencyException(failedAsteriskRecordingDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertRecordingAsync(someBridgeId, someRecordingName, someFormat))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<RecordingInfo> insertTask = this.asteriskCallCenterFoundationService.InsertRecordingAsync(
            someBridgeId, someRecordingName, someFormat);

        RecordingDependencyException actualException =
            await Assert.ThrowsAsync<RecordingDependencyException>(insertTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertRecordingAsync(someBridgeId, someRecordingName, someFormat),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> RecordingNonCriticalDependencyExceptions() =>
    [
        new HttpResponseInternalServerErrorException(),
        new HttpResponseServiceUnavailableException()
    ];

    [Theory]
    [MemberData(nameof(RecordingNonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnInsertRecordingIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someBridgeId = GetRandomString();
        string someRecordingName = GetRandomString();
        string someFormat = GetRandomString();

        var failedAsteriskRecordingDependencyException =
            new FailedAsteriskRecordingDependencyException(dependencyException);

        var expectedException = new RecordingDependencyException(failedAsteriskRecordingDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertRecordingAsync(someBridgeId, someRecordingName, someFormat))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<RecordingInfo> insertTask = this.asteriskCallCenterFoundationService.InsertRecordingAsync(
            someBridgeId, someRecordingName, someFormat);

        RecordingDependencyException actualException =
            await Assert.ThrowsAsync<RecordingDependencyException>(insertTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertRecordingAsync(someBridgeId, someRecordingName, someFormat),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnInsertRecordingIfErrorOccursAndLogItAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someRecordingName = GetRandomString();
        string someFormat = GetRandomString();
        var exception = new Exception();
        var failedRecordingServiceException = new FailedRecordingServiceException(exception);
        var expectedException = new RecordingServiceException(failedRecordingServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertRecordingAsync(someBridgeId, someRecordingName, someFormat))
                .ThrowsAsync(exception);

        // when
        ValueTask<RecordingInfo> insertTask = this.asteriskCallCenterFoundationService.InsertRecordingAsync(
            someBridgeId, someRecordingName, someFormat);

        RecordingServiceException actualException =
            await Assert.ThrowsAsync<RecordingServiceException>(insertTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertRecordingAsync(someBridgeId, someRecordingName, someFormat),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
