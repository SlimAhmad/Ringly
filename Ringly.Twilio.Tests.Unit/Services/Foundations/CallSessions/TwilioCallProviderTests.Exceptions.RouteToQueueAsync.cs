using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Models;
using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnRouteToQueueIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        Guid someCustomerId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        SipCredentials someCredentials = CreateRandomSipCredentials();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidCallParticipantException = new InvalidCallParticipantException();

        var expectedException =
            new CallSessionDependencyValidationException(invalidCallParticipantException);

        this.sipCredentialsStoreMock.Setup(store =>
            store.RetrieveByClientIdAsync(someCustomerId))
                .ReturnsAsync(someCredentials);

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(someQueueName, It.IsAny<TwilioParticipantConfig>()))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<CallSession> routeTask =
            this.twilioCallProvider.RouteToQueueAsync(someCustomerId, someQueueName);

        CallSessionDependencyValidationException actualException =
            await Assert.ThrowsAsync<CallSessionDependencyValidationException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(someQueueName, It.IsAny<TwilioParticipantConfig>()),
                Times.Once);

        this.sipCredentialsStoreMock.Verify(store =>
            store.RetrieveByClientIdAsync(someCustomerId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnRouteToQueueIfUnauthorizedErrorOccursAndLogItAsync()
    {
        // given
        Guid someCustomerId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        SipCredentials someCredentials = CreateRandomSipCredentials();
        var httpResponseUnauthorizedException = new HttpResponseUnauthorizedException();

        var failedTwilioCallProviderDependencyException =
            new FailedTwilioCallProviderDependencyException(httpResponseUnauthorizedException);

        var expectedException =
            new CallProviderDependencyException(failedTwilioCallProviderDependencyException);

        this.sipCredentialsStoreMock.Setup(store =>
            store.RetrieveByClientIdAsync(someCustomerId))
                .ReturnsAsync(someCredentials);

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(someQueueName, It.IsAny<TwilioParticipantConfig>()))
                .ThrowsAsync(httpResponseUnauthorizedException);

        // when
        ValueTask<CallSession> routeTask =
            this.twilioCallProvider.RouteToQueueAsync(someCustomerId, someQueueName);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(someQueueName, It.IsAny<TwilioParticipantConfig>()),
                Times.Once);

        this.sipCredentialsStoreMock.Verify(store =>
            store.RetrieveByClientIdAsync(someCustomerId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
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
            this.twilioCallProvider.RouteToQueueAsync(someCustomerId, someQueueName);

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

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
