using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnConnectAgentToQueueIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someAgentExtension = GetRandomString();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidCallParticipantException = new InvalidCallParticipantException();

        var expectedException =
            new CallSessionDependencyValidationException(invalidCallParticipantException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<Channel> connectTask =
            this.callFoundationService.ConnectAgentToQueueAsync(someBridgeId, someAgentExtension);

        CallSessionDependencyValidationException actualException =
            await Assert.ThrowsAsync<CallSessionDependencyValidationException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(CriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnConnectAgentToQueueIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someBridgeId = GetRandomString();
        string someAgentExtension = GetRandomString();

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(dependencyException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<Channel> connectTask =
            this.callFoundationService.ConnectAgentToQueueAsync(someBridgeId, someAgentExtension);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(NonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnConnectAgentToQueueIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someBridgeId = GetRandomString();
        string someAgentExtension = GetRandomString();

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(dependencyException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<Channel> connectTask =
            this.callFoundationService.ConnectAgentToQueueAsync(someBridgeId, someAgentExtension);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnConnectAgentToQueueIfErrorOccursAndLogItAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someAgentExtension = GetRandomString();
        var exception = new Exception();
        var failedCallProviderServiceException = new FailedCallProviderServiceException(exception);

        var expectedException =
            new CallProviderServiceException(failedCallProviderServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"))
                .ThrowsAsync(exception);

        // when
        ValueTask<Channel> connectTask =
            this.callFoundationService.ConnectAgentToQueueAsync(someBridgeId, someAgentExtension);

        CallProviderServiceException actualException =
            await Assert.ThrowsAsync<CallProviderServiceException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync($"PJSIP/{someAgentExtension}"),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
