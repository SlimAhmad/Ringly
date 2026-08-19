using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnClaimCallIfChannelIdIsInvalidAndLogItAsync(
        string? invalidChannelId)
    {
        // given
        string someAgentAppName = GetRandomString();
        var invalidAgentRequestException = new InvalidAgentRequestException();

        invalidAgentRequestException.UpsertDataList(
            key: "channelId",
            value: "Value is required");

        var expectedValidationException = new AgentValidationException(invalidAgentRequestException);

        // when
        ValueTask<ClaimResult> claimTask =
            this.asteriskCallCenterFoundationService.ClaimCallAsync(invalidChannelId!, someAgentAppName);

        AgentValidationException actualException =
            await Assert.ThrowsAsync<AgentValidationException>(claimTask.AsTask);

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
    public async Task ShouldThrowValidationExceptionOnClaimCallIfAgentAppNameIsInvalidAndLogItAsync(
        string? invalidAgentAppName)
    {
        // given
        string someChannelId = GetRandomString();
        var invalidAgentRequestException = new InvalidAgentRequestException();

        invalidAgentRequestException.UpsertDataList(
            key: "agentAppName",
            value: "Value is required");

        var expectedValidationException = new AgentValidationException(invalidAgentRequestException);

        // when
        ValueTask<ClaimResult> claimTask =
            this.asteriskCallCenterFoundationService.ClaimCallAsync(someChannelId, invalidAgentAppName!);

        AgentValidationException actualException =
            await Assert.ThrowsAsync<AgentValidationException>(claimTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
