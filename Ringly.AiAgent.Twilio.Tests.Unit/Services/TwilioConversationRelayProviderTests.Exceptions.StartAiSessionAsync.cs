using FluentAssertions;
using Moq;
using Ringly.AiAgent.Abstractions.Models;
using Ringly.AiAgent.Twilio.Models.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.AiAgent.Twilio.Tests.Unit.Services;

public partial class TwilioConversationRelayProviderTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnStartAiSessionIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        AiAgentConfig someConfig = CreateRandomAiAgentConfig();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidAiAgentSessionRequestException = new InvalidAiAgentSessionRequestException();

        var expectedDependencyValidationException =
            new AiAgentSessionDependencyValidationException(invalidAiAgentSessionRequestException);

        this.twilioBrokerMock.Setup(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<AiAgentSession> startTask =
            this.provider.StartAiSessionAsync(someChannelId, someConfig);

        AiAgentSessionDependencyValidationException actualException =
            await Assert.ThrowsAsync<AiAgentSessionDependencyValidationException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()),
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
    public async Task ShouldThrowCriticalDependencyExceptionOnStartAiSessionIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someChannelId = GetRandomString();
        AiAgentConfig someConfig = CreateRandomAiAgentConfig();

        var failedTwilioAiAgentDependencyException =
            new FailedTwilioAiAgentDependencyException(dependencyException);

        var expectedDependencyException =
            new AiAgentSessionDependencyException(failedTwilioAiAgentDependencyException);

        this.twilioBrokerMock.Setup(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<AiAgentSession> startTask =
            this.provider.StartAiSessionAsync(someChannelId, someConfig);

        AiAgentSessionDependencyException actualException =
            await Assert.ThrowsAsync<AiAgentSessionDependencyException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedDependencyException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()),
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
    public async Task ShouldThrowDependencyExceptionOnStartAiSessionIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someChannelId = GetRandomString();
        AiAgentConfig someConfig = CreateRandomAiAgentConfig();

        var failedTwilioAiAgentDependencyException =
            new FailedTwilioAiAgentDependencyException(dependencyException);

        var expectedDependencyException =
            new AiAgentSessionDependencyException(failedTwilioAiAgentDependencyException);

        this.twilioBrokerMock.Setup(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<AiAgentSession> startTask =
            this.provider.StartAiSessionAsync(someChannelId, someConfig);

        AiAgentSessionDependencyException actualException =
            await Assert.ThrowsAsync<AiAgentSessionDependencyException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedDependencyException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnStartAiSessionIfErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        AiAgentConfig someConfig = CreateRandomAiAgentConfig();
        var exception = new Exception();

        var failedAiAgentServiceException = new FailedAiAgentServiceException(exception);
        var expectedServiceException = new AiAgentSessionServiceException(failedAiAgentServiceException);

        this.twilioBrokerMock.Setup(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()))
                .ThrowsAsync(exception);

        // when
        ValueTask<AiAgentSession> startTask =
            this.provider.StartAiSessionAsync(someChannelId, someConfig);

        AiAgentSessionServiceException actualException =
            await Assert.ThrowsAsync<AiAgentSessionServiceException>(startTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedServiceException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

        this.twilioBrokerMock.Verify(broker =>
            broker.RedirectCallAsync(someChannelId, It.IsAny<string>()),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
