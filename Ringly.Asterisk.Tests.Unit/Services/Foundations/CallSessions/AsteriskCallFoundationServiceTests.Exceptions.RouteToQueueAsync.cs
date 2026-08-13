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
    public async Task ShouldThrowDependencyValidationExceptionOnRouteToQueueIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        Guid someCustomerId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        SipCredentials someCredentials = CreateRandomSipCredentials();
        HoldingBridge someHoldingBridge = CreateRandomHoldingBridge();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidCallParticipantException = new InvalidCallParticipantException();

        var expectedException =
            new CallSessionDependencyValidationException(invalidCallParticipantException);

        this.sipCredentialsStoreMock.Setup(store =>
            store.RetrieveByClientIdAsync(someCustomerId))
                .ReturnsAsync(someCredentials);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(someHoldingBridge);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync(someCredentials.Extension))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<CallSession> routeTask =
            this.callFoundationService.RouteToQueueAsync(someCustomerId, someQueueName);

        CallSessionDependencyValidationException actualException =
            await Assert.ThrowsAsync<CallSessionDependencyValidationException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync(someCredentials.Extension),
                Times.Once);

        this.sipCredentialsStoreMock.Verify(store =>
            store.RetrieveByClientIdAsync(someCustomerId),
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
    public async Task ShouldThrowCriticalDependencyExceptionOnRouteToQueueIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        Guid someCustomerId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        SipCredentials someCredentials = CreateRandomSipCredentials();
        HoldingBridge someHoldingBridge = CreateRandomHoldingBridge();

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(dependencyException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.sipCredentialsStoreMock.Setup(store =>
            store.RetrieveByClientIdAsync(someCustomerId))
                .ReturnsAsync(someCredentials);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(someHoldingBridge);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync(someCredentials.Extension))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<CallSession> routeTask =
            this.callFoundationService.RouteToQueueAsync(someCustomerId, someQueueName);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync(someCredentials.Extension),
                Times.Once);

        this.sipCredentialsStoreMock.Verify(store =>
            store.RetrieveByClientIdAsync(someCustomerId),
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
    public async Task ShouldThrowDependencyExceptionOnRouteToQueueIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        Guid someCustomerId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        SipCredentials someCredentials = CreateRandomSipCredentials();
        HoldingBridge someHoldingBridge = CreateRandomHoldingBridge();

        var failedAsteriskCallProviderDependencyException =
            new FailedAsteriskCallProviderDependencyException(dependencyException);

        var expectedException =
            new CallProviderDependencyException(failedAsteriskCallProviderDependencyException);

        this.sipCredentialsStoreMock.Setup(store =>
            store.RetrieveByClientIdAsync(someCustomerId))
                .ReturnsAsync(someCredentials);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(someHoldingBridge);

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertChannelAsync(someCredentials.Extension))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<CallSession> routeTask =
            this.callFoundationService.RouteToQueueAsync(someCustomerId, someQueueName);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertChannelAsync(someCredentials.Extension),
                Times.Once);

        this.sipCredentialsStoreMock.Verify(store =>
            store.RetrieveByClientIdAsync(someCustomerId),
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
    public async Task ShouldThrowServiceExceptionOnRouteToQueueIfErrorOccursAndLogItAsync()
    {
        // given
        Guid someCustomerId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        var exception = new Exception();
        var failedCallProviderServiceException = new FailedCallProviderServiceException(exception);

        var expectedException =
            new CallProviderServiceException(failedCallProviderServiceException);

        this.sipCredentialsStoreMock.Setup(store =>
            store.RetrieveByClientIdAsync(someCustomerId))
                .ThrowsAsync(exception);

        // when
        ValueTask<CallSession> routeTask =
            this.callFoundationService.RouteToQueueAsync(someCustomerId, someQueueName);

        CallProviderServiceException actualException =
            await Assert.ThrowsAsync<CallProviderServiceException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.sipCredentialsStoreMock.Verify(store =>
            store.RetrieveByClientIdAsync(someCustomerId),
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
