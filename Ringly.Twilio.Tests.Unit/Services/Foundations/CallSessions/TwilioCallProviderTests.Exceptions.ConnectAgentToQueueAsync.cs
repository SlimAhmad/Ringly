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
    public async Task ShouldThrowDependencyValidationExceptionOnConnectAgentToQueueIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someAgentExtension = GetRandomString();
        string someCustomerChannelId = GetRandomString();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidCallParticipantException = new InvalidCallParticipantException();

        var expectedException =
            new CallSessionDependencyValidationException(invalidCallParticipantException);

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(someBridgeId, It.IsAny<TwilioParticipantConfig>()))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<Channel> connectTask =
            this.twilioCallProvider.ConnectAgentToQueueAsync(someBridgeId, someCustomerChannelId, someAgentExtension);

        CallSessionDependencyValidationException actualException =
            await Assert.ThrowsAsync<CallSessionDependencyValidationException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(someBridgeId, It.IsAny<TwilioParticipantConfig>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnConnectAgentToQueueIfUnauthorizedErrorOccursAndLogItAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someAgentExtension = GetRandomString();
        string someCustomerChannelId = GetRandomString();
        var httpResponseUnauthorizedException = new HttpResponseUnauthorizedException();

        var failedTwilioCallProviderDependencyException =
            new FailedTwilioCallProviderDependencyException(httpResponseUnauthorizedException);

        var expectedException =
            new CallProviderDependencyException(failedTwilioCallProviderDependencyException);

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(someBridgeId, It.IsAny<TwilioParticipantConfig>()))
                .ThrowsAsync(httpResponseUnauthorizedException);

        // when
        ValueTask<Channel> connectTask =
            this.twilioCallProvider.ConnectAgentToQueueAsync(someBridgeId, someCustomerChannelId, someAgentExtension);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(someBridgeId, It.IsAny<TwilioParticipantConfig>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnConnectAgentToQueueIfErrorOccursAndLogItAsync()
    {
        // given
        string someBridgeId = GetRandomString();
        string someAgentExtension = GetRandomString();
        string someCustomerChannelId = GetRandomString();
        var exception = new Exception();
        var failedCallProviderServiceException = new FailedCallProviderServiceException(exception);

        var expectedException =
            new CallProviderServiceException(failedCallProviderServiceException);

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(someBridgeId, It.IsAny<TwilioParticipantConfig>()))
                .ThrowsAsync(exception);

        // when
        ValueTask<Channel> connectTask =
            this.twilioCallProvider.ConnectAgentToQueueAsync(someBridgeId, someCustomerChannelId, someAgentExtension);

        CallProviderServiceException actualException =
            await Assert.ThrowsAsync<CallProviderServiceException>(connectTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(someBridgeId, It.IsAny<TwilioParticipantConfig>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
