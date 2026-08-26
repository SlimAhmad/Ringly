using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnConnectAgentToBridgeIfBridgeIdIsInvalidAndLogItAsync(
        string? invalidBridgeId)
    {
        // given
        string someAgentExtension = GetRandomString();

        var invalidConnectAgentToBridgeRequestException = new InvalidConnectAgentToBridgeRequestException();

        invalidConnectAgentToBridgeRequestException.UpsertDataList(
            key: "bridgeId",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidConnectAgentToBridgeRequestException);

        // when
        ValueTask<AgentConnection> connectTask =
            this.callFoundationService.ConnectAgentToBridgeAsync(invalidBridgeId!, someAgentExtension);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnConnectAgentToBridgeIfAgentExtensionIsInvalidAndLogItAsync(
        string? invalidAgentExtension)
    {
        // given
        string someBridgeId = GetRandomString();

        var invalidConnectAgentToBridgeRequestException = new InvalidConnectAgentToBridgeRequestException();

        invalidConnectAgentToBridgeRequestException.UpsertDataList(
            key: "agentExtension",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidConnectAgentToBridgeRequestException);

        // when
        ValueTask<AgentConnection> connectTask =
            this.callFoundationService.ConnectAgentToBridgeAsync(someBridgeId, invalidAgentExtension!);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
