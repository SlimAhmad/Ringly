using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Tests.Unit.Services.Foundations.Trunks;

public partial class SipTrunkFoundationServiceTests
{
    [Fact]
    public async Task ShouldDialOutAsync()
    {
        // given
        SipTrunkConfig config = CreateRandomSipTrunkConfig();
        string phoneNumber = CreateRandomDomesticPhoneNumber();
        TrunkCallLimitStatus status = CreateRandomWithinLimitsStatus(config.TrunkName);
        var expectedChannel = new Channel { ChannelId = GetRandomString() };

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName))
                .ReturnsAsync(config);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName))
                .ReturnsAsync(status);

        this.sipTrunkBrokerMock.Setup(broker =>
            broker.DialOutAsync(phoneNumber, config.TrunkName))
                .ReturnsAsync(expectedChannel);

        // when
        Channel actualChannel =
            await this.sipTrunkFoundationService.DialOutAsync(phoneNumber, config.TrunkName);

        // then
        actualChannel.Should().BeEquivalentTo(expectedChannel);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveTrunkConfigAsync(config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.RetrieveSpendStatusAsync(config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.Verify(broker =>
            broker.DialOutAsync(phoneNumber, config.TrunkName),
                Times.Once);

        this.sipTrunkBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
