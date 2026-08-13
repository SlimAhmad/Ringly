using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models.Orchestrations.MaskedCalls.Exceptions;

namespace Ringly.Trunking.Asterisk.Tests.Unit.Services.Orchestrations.MaskedCalls;

public partial class MaskedCallOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnHandleInboundTrunkCallIfTrunkEventIsNullAndLogItAsync()
    {
        // given
        TrunkCallEvent nullTrunkEvent = null!;
        var invalidMaskedCallRequestException = new InvalidMaskedCallRequestException();
        var expectedValidationException = new MaskedCallValidationException(invalidMaskedCallRequestException);

        // when
        ValueTask<CallSession> handleTask =
            this.maskedCallOrchestrationService.HandleInboundTrunkCallAsync(nullTrunkEvent);

        MaskedCallValidationException actualException =
            await Assert.ThrowsAsync<MaskedCallValidationException>(handleTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.maskingSessionStoreMock.VerifyNoOtherCalls();
        this.callProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, "channel")]
    [InlineData("", "channel")]
    [InlineData(" ", "channel")]
    [InlineData("+15555550199", null)]
    [InlineData("+15555550199", "")]
    [InlineData("+15555550199", " ")]
    public async Task ShouldThrowValidationExceptionOnHandleInboundTrunkCallIfEventIsInvalidAndLogItAsync(
        string? dialedNumber, string? channelId)
    {
        // given
        var invalidTrunkEvent = new TrunkCallEvent
        {
            TrunkName = GetRandomString(),
            CallerNumber = "+15555550100",
            DialedNumber = dialedNumber!,
            ChannelId = channelId!
        };

        var invalidMaskedCallRequestException = new InvalidMaskedCallRequestException();

        if (string.IsNullOrWhiteSpace(dialedNumber))
        {
            invalidMaskedCallRequestException.UpsertDataList(
                key: nameof(TrunkCallEvent.DialedNumber),
                value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            invalidMaskedCallRequestException.UpsertDataList(
                key: nameof(TrunkCallEvent.ChannelId),
                value: "Value is required");
        }

        var expectedValidationException = new MaskedCallValidationException(invalidMaskedCallRequestException);

        // when
        ValueTask<CallSession> handleTask =
            this.maskedCallOrchestrationService.HandleInboundTrunkCallAsync(invalidTrunkEvent);

        MaskedCallValidationException actualException =
            await Assert.ThrowsAsync<MaskedCallValidationException>(handleTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.maskingSessionStoreMock.VerifyNoOtherCalls();
        this.callProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnHandleInboundTrunkCallIfNoSessionFoundAndLogItAsync()
    {
        // given
        TrunkCallEvent trunkEvent = CreateRandomTrunkCallEvent();
        MaskingSession? nullSession = null;

        var maskingSessionNotFoundException =
            new MaskingSessionNotFoundException(trunkEvent.DialedNumber);

        var expectedValidationException = new MaskedCallValidationException(maskingSessionNotFoundException);

        this.maskingSessionStoreMock.Setup(store =>
            store.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber))
                .ReturnsAsync(nullSession);

        // when
        ValueTask<CallSession> handleTask =
            this.maskedCallOrchestrationService.HandleInboundTrunkCallAsync(trunkEvent);

        MaskedCallValidationException actualException =
            await Assert.ThrowsAsync<MaskedCallValidationException>(handleTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.maskingSessionStoreMock.Verify(store =>
            store.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber),
                Times.Once);

        this.maskingSessionStoreMock.VerifyNoOtherCalls();
        this.callProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnHandleInboundTrunkCallIfSessionExpiredAndLogItAsync()
    {
        // given
        TrunkCallEvent trunkEvent = CreateRandomTrunkCallEvent();
        MaskingSession expiredSession = CreateRandomExpiredMaskingSession(trunkEvent.DialedNumber);

        var maskingSessionNotFoundException =
            new MaskingSessionNotFoundException(trunkEvent.DialedNumber);

        var expectedValidationException = new MaskedCallValidationException(maskingSessionNotFoundException);

        this.maskingSessionStoreMock.Setup(store =>
            store.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber))
                .ReturnsAsync(expiredSession);

        // when
        ValueTask<CallSession> handleTask =
            this.maskedCallOrchestrationService.HandleInboundTrunkCallAsync(trunkEvent);

        MaskedCallValidationException actualException =
            await Assert.ThrowsAsync<MaskedCallValidationException>(handleTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.maskingSessionStoreMock.Verify(store =>
            store.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber),
                Times.Once);

        this.maskingSessionStoreMock.VerifyNoOtherCalls();
        this.callProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
