using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldClaimCallAsync()
    {
        // given
        string inputChannelId = GetRandomString();
        string inputAgentAppName = GetRandomString();
        ClaimResult expectedClaimResult = CreateRandomClaimResult();

        this.asteriskBrokerMock.Setup(broker =>
            broker.ClaimCallAsync(inputChannelId, inputAgentAppName))
                .ReturnsAsync(expectedClaimResult);

        // when
        ClaimResult actualClaimResult =
            await this.asteriskCallCenterFoundationService.ClaimCallAsync(inputChannelId, inputAgentAppName);

        // then
        actualClaimResult.Should().BeEquivalentTo(expectedClaimResult);

        this.asteriskBrokerMock.Verify(broker =>
            broker.ClaimCallAsync(inputChannelId, inputAgentAppName),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
