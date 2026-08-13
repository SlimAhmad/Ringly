using FluentAssertions;
using Moq;
using Ringly.Abstractions.Models;
using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Twilio.Tests.Unit.Services.Foundations.CallSessions;

public partial class TwilioCallProviderTests
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
            this.twilioCallProvider.RouteToQueueAsync(invalidCustomerId, someQueueName);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
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
            this.twilioCallProvider.RouteToQueueAsync(someCustomerId, invalidQueueName!);

        CallSessionValidationException actualException =
            await Assert.ThrowsAsync<CallSessionValidationException>(routeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
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
            this.twilioCallProvider.RouteToQueueAsync(someCustomerId, someQueueName);

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

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.sipCredentialsStoreMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
