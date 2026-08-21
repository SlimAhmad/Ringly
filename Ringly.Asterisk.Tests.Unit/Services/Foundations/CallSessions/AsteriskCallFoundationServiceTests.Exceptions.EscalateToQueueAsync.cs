using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;
using Ringly.CallCenter.Abstractions.Models;
using RESTFulSense.Exceptions;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnEscalateToQueueIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        string someQueueName = GetRandomString();
        HoldingBridge someHoldingBridge = CreateRandomHoldingBridge();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidCallParticipantException = new InvalidCallParticipantException();

        var expectedException =
            new CallSessionDependencyValidationException(invalidCallParticipantException);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(someHoldingBridge);

        this.asteriskBrokerMock.Setup(broker =>
            broker.MoveChannelAsync(someChannelId))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<CallSession> escalateTask =
            this.callFoundationService.EscalateToQueueAsync(someChannelId, someQueueName);

        CallSessionDependencyValidationException actualException =
            await Assert.ThrowsAsync<CallSessionDependencyValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.MoveChannelAsync(someChannelId),
                Times.Once);

        this.queueRegistryMock.Verify(registry =>
            registry.RetrieveByNameAsync(someQueueName),
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
    public async Task ShouldThrowCriticalDependencyExceptionOnEscalateToQueueIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someChannelId = GetRandomString();
        string someQueueName = GetRandomString();
        HoldingBridge someHoldingBridge = CreateRandomHoldingBridge();

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(dependencyException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(someHoldingBridge);

        this.asteriskBrokerMock.Setup(broker =>
            broker.MoveChannelAsync(someChannelId))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<CallSession> escalateTask =
            this.callFoundationService.EscalateToQueueAsync(someChannelId, someQueueName);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.MoveChannelAsync(someChannelId),
                Times.Once);

        this.queueRegistryMock.Verify(registry =>
            registry.RetrieveByNameAsync(someQueueName),
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
    public async Task ShouldThrowDependencyExceptionOnEscalateToQueueIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someChannelId = GetRandomString();
        string someQueueName = GetRandomString();
        HoldingBridge someHoldingBridge = CreateRandomHoldingBridge();

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(dependencyException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(someHoldingBridge);

        this.asteriskBrokerMock.Setup(broker =>
            broker.MoveChannelAsync(someChannelId))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<CallSession> escalateTask =
            this.callFoundationService.EscalateToQueueAsync(someChannelId, someQueueName);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.MoveChannelAsync(someChannelId),
                Times.Once);

        this.queueRegistryMock.Verify(registry =>
            registry.RetrieveByNameAsync(someQueueName),
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
    public async Task ShouldThrowServiceExceptionOnEscalateToQueueIfErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        string someQueueName = GetRandomString();
        var exception = new Exception();
        var failedCallProviderServiceException = new FailedCallProviderServiceException(exception);

        var expectedException =
            new CallProviderServiceException(failedCallProviderServiceException);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ThrowsAsync(exception);

        // when
        ValueTask<CallSession> escalateTask =
            this.callFoundationService.EscalateToQueueAsync(someChannelId, someQueueName);

        CallProviderServiceException actualException =
            await Assert.ThrowsAsync<CallProviderServiceException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.queueRegistryMock.Verify(registry =>
            registry.RetrieveByNameAsync(someQueueName),
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
