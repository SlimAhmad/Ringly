using FluentAssertions;
using Moq;
using Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnSetAgentAvailabilityIfAgentAppNameIsInvalidAndLogItAsync(
        string? invalidAgentAppName)
    {
        // given
        bool someIsAvailable = true;
        var invalidAgentRequestException = new InvalidAgentRequestException();

        invalidAgentRequestException.UpsertDataList(
            key: "agentAppName",
            value: "Value is required");

        var expectedValidationException = new AgentValidationException(invalidAgentRequestException);

        // when
        ValueTask setTask = this.asteriskCallCenterFoundationService.SetAgentAvailabilityAsync(
            invalidAgentAppName!, someIsAvailable);

        AgentValidationException actualException =
            await Assert.ThrowsAsync<AgentValidationException>(setTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
