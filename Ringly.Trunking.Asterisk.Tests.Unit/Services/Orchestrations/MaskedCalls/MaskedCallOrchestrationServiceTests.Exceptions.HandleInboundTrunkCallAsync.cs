using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models.Orchestrations.MaskedCalls.Exceptions;

namespace Ringly.Trunking.Asterisk.Tests.Unit.Services.Orchestrations.MaskedCalls;

public partial class MaskedCallOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyExceptionOnHandleInboundTrunkCallIfSessionStoreErrorOccursAndLogItAsync()
    {
        // given
        TrunkCallEvent trunkEvent = CreateRandomTrunkCallEvent();
        var exception = new Exception();
        var failedMaskedCallDependencyException = new FailedMaskedCallDependencyException(exception);
        var expectedException = new MaskedCallDependencyException(failedMaskedCallDependencyException);

        this.maskingSessionStoreMock.Setup(store =>
            store.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber))
                .ThrowsAsync(exception);

        // when
        ValueTask<CallSession> handleTask =
            this.maskedCallOrchestrationService.HandleInboundTrunkCallAsync(trunkEvent);

        MaskedCallDependencyException actualException =
            await Assert.ThrowsAsync<MaskedCallDependencyException>(handleTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.maskingSessionStoreMock.Verify(store =>
            store.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber),
                Times.Once);

        this.maskingSessionStoreMock.VerifyNoOtherCalls();
        this.callProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyExceptionOnHandleInboundTrunkCallIfCallProviderErrorOccursAndLogItAsync()
    {
        // given
        TrunkCallEvent trunkEvent = CreateRandomTrunkCallEvent();
        MaskingSession session = CreateRandomActiveMaskingSession(trunkEvent.DialedNumber);
        var exception = new Exception();
        var failedMaskedCallDependencyException = new FailedMaskedCallDependencyException(exception);
        var expectedException = new MaskedCallDependencyException(failedMaskedCallDependencyException);

        this.maskingSessionStoreMock.Setup(store =>
            store.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber))
                .ReturnsAsync(session);

        this.callProviderMock.Setup(provider =>
            provider.StartCallSessionAsync(
                It.IsAny<CallParticipant>(),
                It.IsAny<CallParticipant>()))
                    .ThrowsAsync(exception);

        // when
        ValueTask<CallSession> handleTask =
            this.maskedCallOrchestrationService.HandleInboundTrunkCallAsync(trunkEvent);

        MaskedCallDependencyException actualException =
            await Assert.ThrowsAsync<MaskedCallDependencyException>(handleTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.maskingSessionStoreMock.Verify(store =>
            store.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber),
                Times.Once);

        this.callProviderMock.Verify(provider =>
            provider.StartCallSessionAsync(
                It.IsAny<CallParticipant>(),
                It.IsAny<CallParticipant>()),
                    Times.Once);

        this.maskingSessionStoreMock.VerifyNoOtherCalls();
        this.callProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
