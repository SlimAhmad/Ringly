using FluentAssertions;
using Moq;
using Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnCopyStoredRecordingIfRecordingNameIsInvalidAndLogItAsync(
        string? invalidRecordingName)
    {
        // given
        string someDestinationName = GetRandomString();
        var invalidRecordingRequestException = new InvalidRecordingRequestException();

        invalidRecordingRequestException.UpsertDataList(
            key: "recordingName",
            value: "Value is required");

        var expectedValidationException = new RecordingValidationException(invalidRecordingRequestException);

        // when
        ValueTask copyTask = this.asteriskCallCenterFoundationService.CopyStoredRecordingAsync(
            invalidRecordingName!, someDestinationName);

        RecordingValidationException actualException =
            await Assert.ThrowsAsync<RecordingValidationException>(copyTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnCopyStoredRecordingIfDestinationNameIsInvalidAndLogItAsync(
        string? invalidDestinationName)
    {
        // given
        string someRecordingName = GetRandomString();
        var invalidRecordingRequestException = new InvalidRecordingRequestException();

        invalidRecordingRequestException.UpsertDataList(
            key: "destinationName",
            value: "Value is required");

        var expectedValidationException = new RecordingValidationException(invalidRecordingRequestException);

        // when
        ValueTask copyTask = this.asteriskCallCenterFoundationService.CopyStoredRecordingAsync(
            someRecordingName, invalidDestinationName!);

        RecordingValidationException actualException =
            await Assert.ThrowsAsync<RecordingValidationException>(copyTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
