using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Models;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
{
    [Fact]
    public async Task ShouldStartCallSessionAsync()
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();
        var capturedConferenceNames = new List<string>();

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(
                It.IsAny<string>(),
                It.Is<TwilioParticipantConfig>(config =>
                    config.To == partyA.SipExtension && config.From == DefaultCallerId)))
                        .Callback<string, TwilioParticipantConfig>((conferenceName, _) =>
                            capturedConferenceNames.Add(conferenceName))
                        .ReturnsAsync(new TwilioParticipant());

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(
                It.IsAny<string>(),
                It.Is<TwilioParticipantConfig>(config =>
                    config.To == partyB.SipExtension && config.From == DefaultCallerId)))
                        .Callback<string, TwilioParticipantConfig>((conferenceName, _) =>
                            capturedConferenceNames.Add(conferenceName))
                        .ReturnsAsync(new TwilioParticipant());

        // when
        CallSession actualCallSession =
            await this.twilioCallProvider.StartCallSessionAsync(partyA, partyB);

        // then
        actualCallSession.CallSessionId.Should().NotBeEmpty();
        actualCallSession.BridgeId.Should().NotBeNullOrWhiteSpace();

        // both participants must be dialed into the SAME conference for the "bridge" to work
        capturedConferenceNames.Should().HaveCount(2);
        capturedConferenceNames[0].Should().Be(capturedConferenceNames[1]);
        actualCallSession.BridgeId.Should().Be(capturedConferenceNames[0]);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(
                actualCallSession.BridgeId,
                It.Is<TwilioParticipantConfig>(config => config.To == partyA.SipExtension)),
                    Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(
                actualCallSession.BridgeId,
                It.Is<TwilioParticipantConfig>(config => config.To == partyB.SipExtension)),
                    Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
