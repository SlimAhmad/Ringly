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
    public async Task ShouldThrowDependencyValidationExceptionOnStartIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidCallParticipantException = new InvalidCallParticipantException();

        var expectedException =
            new CallSessionDependencyValidationException(invalidCallParticipantException);

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(
                It.IsAny<string>(),
                It.Is<TwilioParticipantConfig>(config => config.To == partyA.SipExtension)))
                    .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<CallSession> startTask =
            this.twilioCallProvider.StartCallSessionAsync(partyA, partyB);

        CallSessionDependencyValidationException actualException =
            await Assert.ThrowsAsync<CallSessionDependencyValidationException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(
                It.IsAny<string>(),
                It.Is<TwilioParticipantConfig>(config => config.To == partyA.SipExtension)),
                    Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> CriticalDependencyExceptions() =>
    [
        new HttpResponseUnauthorizedException(),
        new HttpResponseForbiddenException(),
        new HttpResponseNotFoundException(),
        new HttpRequestException()
    ];

    [Theory]
    [MemberData(nameof(CriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnStartIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();

        var failedTwilioCallProviderDependencyException =
            new FailedTwilioCallProviderDependencyException(dependencyException);

        var expectedException =
            new CallProviderDependencyException(failedTwilioCallProviderDependencyException);

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(
                It.IsAny<string>(),
                It.Is<TwilioParticipantConfig>(config => config.To == partyA.SipExtension)))
                    .ThrowsAsync(dependencyException);

        // when
        ValueTask<CallSession> startTask =
            this.twilioCallProvider.StartCallSessionAsync(partyA, partyB);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(
                It.IsAny<string>(),
                It.Is<TwilioParticipantConfig>(config => config.To == partyA.SipExtension)),
                    Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> NonCriticalDependencyExceptions() =>
    [
        new HttpResponseInternalServerErrorException(),
        new HttpResponseServiceUnavailableException()
    ];

    [Theory]
    [MemberData(nameof(NonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnStartIfErrorOccursAndLogItAsync(Exception dependencyException)
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();

        var failedTwilioCallProviderDependencyException =
            new FailedTwilioCallProviderDependencyException(dependencyException);

        var expectedException =
            new CallProviderDependencyException(failedTwilioCallProviderDependencyException);

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(
                It.IsAny<string>(),
                It.Is<TwilioParticipantConfig>(config => config.To == partyA.SipExtension)))
                    .ThrowsAsync(dependencyException);

        // when
        ValueTask<CallSession> startTask =
            this.twilioCallProvider.StartCallSessionAsync(partyA, partyB);

        CallProviderDependencyException actualException =
            await Assert.ThrowsAsync<CallProviderDependencyException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(
                It.IsAny<string>(),
                It.Is<TwilioParticipantConfig>(config => config.To == partyA.SipExtension)),
                    Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnStartIfErrorOccursAndLogItAsync()
    {
        // given
        CallParticipant partyA = CreateRandomCallParticipant();
        CallParticipant partyB = CreateRandomCallParticipant();
        var exception = new Exception();
        var failedCallProviderServiceException = new FailedCallProviderServiceException(exception);

        var expectedException =
            new CallProviderServiceException(failedCallProviderServiceException);

        this.twilioBrokerMock.Setup(broker =>
            broker.AddParticipantAsync(
                It.IsAny<string>(),
                It.Is<TwilioParticipantConfig>(config => config.To == partyA.SipExtension)))
                    .ThrowsAsync(exception);

        // when
        ValueTask<CallSession> startTask =
            this.twilioCallProvider.StartCallSessionAsync(partyA, partyB);

        CallProviderServiceException actualException =
            await Assert.ThrowsAsync<CallProviderServiceException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.AddParticipantAsync(
                It.IsAny<string>(),
                It.Is<TwilioParticipantConfig>(config => config.To == partyA.SipExtension)),
                    Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
