using FluentAssertions;
using Moq;
using Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnSetAgentAvailabilityIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string someAgentAppName = GetRandomString();
        bool someIsAvailable = true;
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidAgentRequestException = new InvalidAgentRequestException();
        var expectedException = new AgentDependencyValidationException(invalidAgentRequestException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SetAgentAvailabilityAsync(someAgentAppName, someIsAvailable))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask setTask = this.asteriskCallCenterFoundationService.SetAgentAvailabilityAsync(
            someAgentAppName, someIsAvailable);

        AgentDependencyValidationException actualException =
            await Assert.ThrowsAsync<AgentDependencyValidationException>(setTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.SetAgentAvailabilityAsync(someAgentAppName, someIsAvailable),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(AgentCriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnSetAgentAvailabilityIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someAgentAppName = GetRandomString();
        bool someIsAvailable = true;
        var failedAsteriskAgentDependencyException = new FailedAsteriskAgentDependencyException(dependencyException);
        var expectedException = new AgentDependencyException(failedAsteriskAgentDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SetAgentAvailabilityAsync(someAgentAppName, someIsAvailable))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask setTask = this.asteriskCallCenterFoundationService.SetAgentAvailabilityAsync(
            someAgentAppName, someIsAvailable);

        AgentDependencyException actualException =
            await Assert.ThrowsAsync<AgentDependencyException>(setTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.SetAgentAvailabilityAsync(someAgentAppName, someIsAvailable),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(AgentNonCriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnSetAgentAvailabilityIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string someAgentAppName = GetRandomString();
        bool someIsAvailable = true;
        var failedAsteriskAgentDependencyException = new FailedAsteriskAgentDependencyException(dependencyException);
        var expectedException = new AgentDependencyException(failedAsteriskAgentDependencyException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SetAgentAvailabilityAsync(someAgentAppName, someIsAvailable))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask setTask = this.asteriskCallCenterFoundationService.SetAgentAvailabilityAsync(
            someAgentAppName, someIsAvailable);

        AgentDependencyException actualException =
            await Assert.ThrowsAsync<AgentDependencyException>(setTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.SetAgentAvailabilityAsync(someAgentAppName, someIsAvailable),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnSetAgentAvailabilityIfErrorOccursAndLogItAsync()
    {
        // given
        string someAgentAppName = GetRandomString();
        bool someIsAvailable = true;
        var exception = new Exception();
        var failedAgentServiceException = new FailedAgentServiceException(exception);
        var expectedException = new AgentServiceException(failedAgentServiceException);

        this.asteriskBrokerMock.Setup(broker =>
            broker.SetAgentAvailabilityAsync(someAgentAppName, someIsAvailable))
                .ThrowsAsync(exception);

        // when
        ValueTask setTask = this.asteriskCallCenterFoundationService.SetAgentAvailabilityAsync(
            someAgentAppName, someIsAvailable);

        AgentServiceException actualException =
            await Assert.ThrowsAsync<AgentServiceException>(setTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.asteriskBrokerMock.Verify(broker =>
            broker.SetAgentAvailabilityAsync(someAgentAppName, someIsAvailable),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
