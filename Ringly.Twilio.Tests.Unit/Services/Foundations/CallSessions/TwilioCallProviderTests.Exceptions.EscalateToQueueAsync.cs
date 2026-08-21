using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnEscalateToQueueIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        string someQueueName = GetRandomString();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidCallParticipantException = new InvalidCallParticipantException();

        var expectedException =
            new CallSessionDependencyValidationException(invalidCallParticipantException);

        this.twilioBrokerMock.Setup(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<CallSession> escalateTask =
            this.twilioCallProvider.EscalateToQueueAsync(someChannelId, someQueueName);

        CallSessionDependencyValidationException actualException =
            await Assert.ThrowsAsync<CallSessionDependencyValidationException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.twilioBrokerMock.Verify(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnEscalateToQueueIfUnauthorizedErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        string someQueueName = GetRandomString();
        var httpResponseUnauthorizedException = new HttpResponseUnauthorizedException();

        var failedTwilioCallProviderDependencyException =
            new FailedTwilioCallProviderDependencyException(httpResponseUnauthorizedException);

        var expectedException =
            new CallProviderDependencyException(failedTwilioCallProviderDependencyException);

        this.twilioBrokerMock.Setup(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()))
                .ThrowsAsync(httpResponseUnauthorizedException);

        // when
        ValueTask<CallSession> escalateTask =
            this.twilioCallProvider.EscalateToQueueAsync(someChannelId, someQueueName);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.twilioBrokerMock.Verify(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
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

        this.twilioBrokerMock.Setup(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()))
                .ThrowsAsync(exception);

        // when
        ValueTask<CallSession> escalateTask =
            this.twilioCallProvider.EscalateToQueueAsync(someChannelId, someQueueName);

        CallProviderServiceException actualException =
            await Assert.ThrowsAsync<CallProviderServiceException>(escalateTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.twilioBrokerMock.Verify(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
