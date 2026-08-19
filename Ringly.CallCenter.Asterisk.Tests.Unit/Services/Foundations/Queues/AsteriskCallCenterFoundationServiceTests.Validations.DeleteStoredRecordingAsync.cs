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
    public async Task ShouldThrowValidationExceptionOnDeleteStoredRecordingIfRecordingNameIsInvalidAndLogItAsync(
        string? invalidRecordingName)
    {
        // given
        var invalidRecordingRequestException = new InvalidRecordingRequestException();

        invalidRecordingRequestException.UpsertDataList(
            key: "recordingName",
            value: "Value is required");

        var expectedValidationException = new RecordingValidationException(invalidRecordingRequestException);

        // when
        ValueTask deleteTask =
            this.asteriskCallCenterFoundationService.DeleteStoredRecordingAsync(invalidRecordingName!);

        RecordingValidationException actualException =
            await Assert.ThrowsAsync<RecordingValidationException>(deleteTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
