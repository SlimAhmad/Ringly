using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnConnectAgentToQueueIfBridgeIdIsInvalidAndLogItAsync(
        string? invalidBridgeId)
    {
        // given
        string someAgentExtension = GetRandomString();

        var invalidConnectAgentToQueueRequestException = new InvalidConnectAgentToQueueRequestException();

        invalidConnectAgentToQueueRequestException.UpsertDataList(
            key: "bridgeId",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidConnectAgentToQueueRequestException);

        // when
        ValueTask<Channel> connectTask =
            this.twilioCallProvider.ConnectAgentToQueueAsync(invalidBridgeId!, someAgentExtension);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
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

        var invalidConnectAgentToQueueRequestException = new InvalidConnectAgentToQueueRequestException();

        invalidConnectAgentToQueueRequestException.UpsertDataList(
            key: "agentExtension",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidConnectAgentToQueueRequestException);

        // when
        ValueTask<Channel> connectTask =
            this.twilioCallProvider.ConnectAgentToQueueAsync(someBridgeId, invalidAgentExtension!);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
