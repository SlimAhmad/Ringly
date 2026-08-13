using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.Asterisk.Tests.Unit.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnRouteToQueueIfCustomerIdIsInvalidAndLogItAsync()
    {
        // given
        Guid invalidCustomerId = Guid.Empty;
        string someQueueName = GetRandomString();

        var invalidRouteToQueueRequestException = new InvalidRouteToQueueRequestException();

        invalidRouteToQueueRequestException.UpsertDataList(
            key: "customerId",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidRouteToQueueRequestException);

        // when
        ValueTask<CallSession> routeTask =
            this.callFoundationService.RouteToQueueAsync(invalidCustomerId, someQueueName);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnRouteToQueueIfQueueNameIsInvalidAndLogItAsync(
        string? invalidQueueName)
    {
        // given
        Guid someCustomerId = Guid.NewGuid();

        var invalidRouteToQueueRequestException = new InvalidRouteToQueueRequestException();

        invalidRouteToQueueRequestException.UpsertDataList(
            key: "queueName",
            value: "Value is required");

        var expectedValidationException =
            new CallSessionValidationException(invalidRouteToQueueRequestException);

        // when
        ValueTask<CallSession> routeTask =
            this.callFoundationService.RouteToQueueAsync(someCustomerId, invalidQueueName!);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnRouteToQueueIfSipCredentialsNotFoundAndLogItAsync()
    {
        // given
        Guid someCustomerId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        SipCredentials? nullCredentials = null;

        var notFoundSipCredentialsException = new NotFoundSipCredentialsException(someCustomerId);

        var expectedValidationException =
            new CallSessionValidationException(notFoundSipCredentialsException);

        this.sipCredentialsStoreMock.Setup(store =>
            store.RetrieveByClientIdAsync(someCustomerId))
                .ReturnsAsync(nullCredentials);

        // when
        ValueTask<CallSession> routeTask =
            this.callFoundationService.RouteToQueueAsync(someCustomerId, someQueueName);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.sipCredentialsStoreMock.Verify(store =>
            store.RetrieveByClientIdAsync(someCustomerId),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnRouteToQueueIfQueueNotFoundAndLogItAsync()
    {
        // given
        Guid someCustomerId = Guid.NewGuid();
        string someQueueName = GetRandomString();
        SipCredentials someCredentials = CreateRandomSipCredentials();
        HoldingBridge? nullHoldingBridge = null;

        var notFoundQueueException = new NotFoundQueueException(someQueueName);

        var expectedValidationException =
            new CallSessionValidationException(notFoundQueueException);

        this.sipCredentialsStoreMock.Setup(store =>
            store.RetrieveByClientIdAsync(someCustomerId))
                .ReturnsAsync(someCredentials);

        this.queueRegistryMock.Setup(registry =>
            registry.RetrieveByNameAsync(someQueueName))
                .ReturnsAsync(nullHoldingBridge);

        // when
        ValueTask<CallSession> routeTask =
            this.callFoundationService.RouteToQueueAsync(someCustomerId, someQueueName);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.sipCredentialsStoreMock.Verify(store =>
            store.RetrieveByClientIdAsync(someCustomerId),
                Times.Once);

        this.queueRegistryMock.Verify(registry =>
            registry.RetrieveByNameAsync(someQueueName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
