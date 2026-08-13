using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Tests.Unit.Services.Orchestrations.MaskedCalls;

public partial class MaskedCallOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldHandleInboundTrunkCallAsync()
    {
        // given
        TrunkCallEvent trunkEvent = CreateRandomTrunkCallEvent();
        MaskingSession session = CreateRandomActiveMaskingSession(trunkEvent.DialedNumber);
        var expectedCallSession = new CallSession { CallSessionId = Guid.NewGuid() };

        var expectedOtherParty = new CallParticipant { SipExtension = session.OtherPartyExtension };
        var expectedCaller = new CallParticipant { SipExtension = trunkEvent.ChannelId };

        this.maskingSessionStoreMock.Setup(store =>
            store.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber))
                .ReturnsAsync(session);

        this.callProviderMock.Setup(provider =>
            provider.StartCallSessionAsync(
                It.Is<CallParticipant>(party => party.SipExtension == expectedOtherParty.SipExtension),
                It.Is<CallParticipant>(party => party.SipExtension == expectedCaller.SipExtension)))
                    .ReturnsAsync(expectedCallSession);

        // when
        CallSession actualCallSession =
            await this.maskedCallOrchestrationService.HandleInboundTrunkCallAsync(trunkEvent);

        // then
        actualCallSession.Should().BeEquivalentTo(expectedCallSession);

        this.maskingSessionStoreMock.Verify(store =>
            store.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber),
                Times.Once);

        this.callProviderMock.Verify(provider =>
            provider.StartCallSessionAsync(
                It.Is<CallParticipant>(party => party.SipExtension == expectedOtherParty.SipExtension),
                It.Is<CallParticipant>(party => party.SipExtension == expectedCaller.SipExtension)),
                    Times.Once);

        this.maskingSessionStoreMock.VerifyNoOtherCalls();
        this.callProviderMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
