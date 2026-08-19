using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnClaimCallIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        string someAgentAppName = GetRandomString();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidAgentRequestException = new InvalidAgentRequestException();
        var expectedException = new AgentDependencyValidationException(invalidAgentRequestException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.ClaimCallAsync(someChannelId, someAgentAppName))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<ClaimResult> claimTask =
            this.asteriskCallCenterFoundationService.ClaimCallAsync(someChannelId, someAgentAppName);

        AgentDependencyValidationException actualException =
            await Assert.ThrowsAsync<AgentDependencyValidationException>(claimTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.ClaimCallAsync(someChannelId, someAgentAppName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> AgentCriticalDependencyExceptions() =>
    [
        new HttpResponseUnauthorizedException(),
        new HttpResponseForbiddenException(),
        new HttpResponseNotFoundException(),
        new HttpRequestException()
    ];

    [Theory]
    [MemberData(nameof(AgentCriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnClaimCallIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someChannelId = GetRandomString();
        string someAgentAppName = GetRandomString();
        var failedAsteriskAgentDependencyException = new FailedAsteriskAgentDependencyException(dependencyException);
        var expectedException = new AgentDependencyException(failedAsteriskAgentDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.ClaimCallAsync(someChannelId, someAgentAppName))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<ClaimResult> claimTask =
            this.asteriskCallCenterFoundationService.ClaimCallAsync(someChannelId, someAgentAppName);

        AgentDependencyException actualException =
            await Assert.ThrowsAsync<AgentDependencyException>(claimTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.ClaimCallAsync(someChannelId, someAgentAppName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Exception> AgentNonCriticalDependencyExceptions() =>
    [
        new HttpResponseInternalServerErrorException(),
        new HttpResponseServiceUnavailableException()
    ];

    [Theory]
    [MemberData(nameof(AgentNonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnClaimCallIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someChannelId = GetRandomString();
        string someAgentAppName = GetRandomString();
        var failedAsteriskAgentDependencyException = new FailedAsteriskAgentDependencyException(dependencyException);
        var expectedException = new AgentDependencyException(failedAsteriskAgentDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.ClaimCallAsync(someChannelId, someAgentAppName))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<ClaimResult> claimTask =
            this.asteriskCallCenterFoundationService.ClaimCallAsync(someChannelId, someAgentAppName);

        AgentDependencyException actualException =
            await Assert.ThrowsAsync<AgentDependencyException>(claimTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.ClaimCallAsync(someChannelId, someAgentAppName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnClaimCallIfErrorOccursAndLogItAsync()
    {
        // given
        string someChannelId = GetRandomString();
        string someAgentAppName = GetRandomString();
        var exception = new Exception();
        var failedAgentServiceException = new FailedAgentServiceException(exception);
        var expectedException = new AgentServiceException(failedAgentServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.ClaimCallAsync(someChannelId, someAgentAppName))
                .ThrowsAsync(exception);

        // when
        ValueTask<ClaimResult> claimTask =
            this.asteriskCallCenterFoundationService.ClaimCallAsync(someChannelId, someAgentAppName);

        AgentServiceException actualException =
            await Assert.ThrowsAsync<AgentServiceException>(claimTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.ClaimCallAsync(someChannelId, someAgentAppName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
