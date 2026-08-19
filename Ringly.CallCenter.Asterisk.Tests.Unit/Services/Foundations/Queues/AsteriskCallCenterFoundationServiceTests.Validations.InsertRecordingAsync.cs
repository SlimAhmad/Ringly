using FluentAssertions;
using Moq;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnInsertRecordingIfBridgeIdIsInvalidAndLogItAsync(
        string? invalidBridgeId)
    {
        // given
        string someRecordingName = GetRandomString();
        string someFormat = GetRandomString();
        var invalidRecordingRequestException = new InvalidRecordingRequestException();

        invalidRecordingRequestException.UpsertDataList(
            key: "bridgeId",
            value: "Value is required");

        var expectedValidationException = new RecordingValidationException(invalidRecordingRequestException);

        // when
        ValueTask<RecordingInfo> insertTask = this.asteriskCallCenterFoundationService.InsertRecordingAsync(
            invalidBridgeId!, someRecordingName, someFormat);

        RecordingValidationException actualException =
            await Assert.ThrowsAsync<RecordingValidationException>(insertTask.AsTask);

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
    public async Task ShouldThrowValidationExceptionOnInsertRecordingIfRecordingNameIsInvalidAndLogItAsync(
        string? invalidRecordingName)
    {
        // given
        string someBridgeId = GetRandomString();
        string someFormat = GetRandomString();
        var invalidRecordingRequestException = new InvalidRecordingRequestException();

        invalidRecordingRequestException.UpsertDataList(
            key: "recordingName",
            value: "Value is required");

        var expectedValidationException = new RecordingValidationException(invalidRecordingRequestException);

        // when
        ValueTask<RecordingInfo> insertTask = this.asteriskCallCenterFoundationService.InsertRecordingAsync(
            someBridgeId, invalidRecordingName!, someFormat);

        RecordingValidationException actualException =
            await Assert.ThrowsAsync<RecordingValidationException>(insertTask.AsTask);

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
    public async Task ShouldThrowValidationExceptionOnInsertRecordingIfFormatIsInvalidAndLogItAsync(
        string? invalidFormat)
    {
        // given
        string someBridgeId = GetRandomString();
        string someRecordingName = GetRandomString();
        var invalidRecordingRequestException = new InvalidRecordingRequestException();

        invalidRecordingRequestException.UpsertDataList(
            key: "format",
            value: "Value is required");

        var expectedValidationException = new RecordingValidationException(invalidRecordingRequestException);

        // when
        ValueTask<RecordingInfo> insertTask = this.asteriskCallCenterFoundationService.InsertRecordingAsync(
            someBridgeId, someRecordingName, invalidFormat!);

        RecordingValidationException actualException =
            await Assert.ThrowsAsync<RecordingValidationException>(insertTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
