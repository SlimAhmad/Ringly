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
    public async Task ShouldThrowValidationExceptionOnConnectAgentToQueueIfBridgeIdIsInvalidAndLogItAsync(
        string? invalidBridgeId)
    {
        // given
        string someCustomerChannelId = GetRandomString();
        string someAgentExtension = GetRandomString();

        var invalidConnectAgentToQueueRequestException = new InvalidConnectAgentToQueueRequestException();

        invalidConnectAgentToQueueRequestException.UpsertDataList(
            key: "bridgeId",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidConnectAgentToQueueRequestException);

        // when
        ValueTask<AgentConnection> connectTask = this.callFoundationService.ConnectAgentToQueueAsync(
            invalidBridgeId!, someCustomerChannelId, someAgentExtension);

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
    public async Task ShouldThrowValidationExceptionOnConnectAgentToQueueIfCustomerChannelIdIsInvalidAndLogItAsync(
        string? invalidCustomerChannelId)
    {
        // given
        string someBridgeId = GetRandomString();
        string someAgentExtension = GetRandomString();

        var invalidConnectAgentToQueueRequestException = new InvalidConnectAgentToQueueRequestException();

        invalidConnectAgentToQueueRequestException.UpsertDataList(
            key: "customerChannelId",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidConnectAgentToQueueRequestException);

        // when
        ValueTask<AgentConnection> connectTask = this.callFoundationService.ConnectAgentToQueueAsync(
            someBridgeId, invalidCustomerChannelId!, someAgentExtension);

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
    public async Task ShouldThrowValidationExceptionOnConnectAgentToQueueIfAgentExtensionIsInvalidAndLogItAsync(
        string? invalidAgentExtension)
    {
        // given
        string someBridgeId = GetRandomString();
        string someCustomerChannelId = GetRandomString();

        var invalidConnectAgentToQueueRequestException = new InvalidConnectAgentToQueueRequestException();

        invalidConnectAgentToQueueRequestException.UpsertDataList(
            key: "agentExtension",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidConnectAgentToQueueRequestException);

        // when
        ValueTask<AgentConnection> connectTask = this.callFoundationService.ConnectAgentToQueueAsync(
            someBridgeId, someCustomerChannelId, invalidAgentExtension!);

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
