using FluentAssertions;
using Moq;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Twilio.Models.Foundations.Queues.Exceptions;
using RESTFulSense.Exceptions;

namespace Ringly.CallCenter.Twilio.Tests.Unit.Services.Foundations.Queues;

public partial class TwilioCallCenterProviderTests
{
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnCreateQueueIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();
        var httpResponseBadRequestException = new HttpResponseBadRequestException();
        var invalidQueueConfigException = new InvalidQueueConfigException();

        var expectedQueueConfigDependencyValidationException =
            new QueueConfigDependencyValidationException(invalidQueueConfigException);

        this.twilioBrokerMock.Setup(broker =>
            broker.InsertTaskQueueAsync(someQueueConfig.Name))
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.twilioCallCenterProvider.CreateQueueAsync(someQueueConfig);

        QueueConfigDependencyValidationException actualException =
            await Assert.ThrowsAsync<QueueConfigDependencyValidationException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigDependencyValidationException);

        this.twilioBrokerMock.Verify(broker =>
            broker.InsertTaskQueueAsync(someQueueConfig.Name),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedQueueConfigDependencyValidationException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnCreateQueueIfConflictErrorOccursAndLogItAsync()
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();
        var httpResponseConflictException = new HttpResponseConflictException();
        var alreadyExistsQueueConfigException = new AlreadyExistsQueueConfigException(httpResponseConflictException);

        var expectedQueueConfigDependencyValidationException =
            new QueueConfigDependencyValidationException(alreadyExistsQueueConfigException);

        this.twilioBrokerMock.Setup(broker =>
            broker.InsertTaskQueueAsync(someQueueConfig.Name))
                .ThrowsAsync(httpResponseConflictException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.twilioCallCenterProvider.CreateQueueAsync(someQueueConfig);

        QueueConfigDependencyValidationException actualException =
            await Assert.ThrowsAsync<QueueConfigDependencyValidationException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigDependencyValidationException);

        this.twilioBrokerMock.Verify(broker =>
            broker.InsertTaskQueueAsync(someQueueConfig.Name),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedQueueConfigDependencyValidationException))),
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
    public async Task ShouldThrowCriticalDependencyExceptionOnCreateQueueIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();

        var failedTwilioQueueConfigDependencyException =
            new FailedTwilioQueueConfigDependencyException(dependencyException);

        var expectedQueueConfigDependencyException =
            new QueueConfigDependencyException(failedTwilioQueueConfigDependencyException);

        this.twilioBrokerMock.Setup(broker =>
            broker.InsertTaskQueueAsync(someQueueConfig.Name))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.twilioCallCenterProvider.CreateQueueAsync(someQueueConfig);

        QueueConfigDependencyException actualException =
            await Assert.ThrowsAsync<QueueConfigDependencyException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigDependencyException);

        this.twilioBrokerMock.Verify(broker =>
            broker.InsertTaskQueueAsync(someQueueConfig.Name),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(expectedQueueConfigDependencyException))),
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
    public async Task ShouldThrowDependencyExceptionOnCreateQueueIfErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();

        var failedTwilioQueueConfigDependencyException =
            new FailedTwilioQueueConfigDependencyException(dependencyException);

        var expectedQueueConfigDependencyException =
            new QueueConfigDependencyException(failedTwilioQueueConfigDependencyException);

        this.twilioBrokerMock.Setup(broker =>
            broker.InsertTaskQueueAsync(someQueueConfig.Name))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.twilioCallCenterProvider.CreateQueueAsync(someQueueConfig);

        QueueConfigDependencyException actualException =
            await Assert.ThrowsAsync<QueueConfigDependencyException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigDependencyException);

        this.twilioBrokerMock.Verify(broker =>
            broker.InsertTaskQueueAsync(someQueueConfig.Name),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedQueueConfigDependencyException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnCreateQueueIfErrorOccursAndLogItAsync()
    {
        // given
        QueueConfig someQueueConfig = CreateRandomQueueConfig();
        var exception = new Exception();

        var failedQueueConfigServiceException =
            new FailedQueueConfigServiceException(exception);

        var expectedQueueConfigServiceException =
            new QueueConfigServiceException(failedQueueConfigServiceException);

        this.twilioBrokerMock.Setup(broker =>
            broker.InsertTaskQueueAsync(someQueueConfig.Name))
                .ThrowsAsync(exception);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.twilioCallCenterProvider.CreateQueueAsync(someQueueConfig);

        QueueConfigServiceException actualException =
            await Assert.ThrowsAsync<QueueConfigServiceException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigServiceException);

        this.twilioBrokerMock.Verify(broker =>
            broker.InsertTaskQueueAsync(someQueueConfig.Name),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedQueueConfigServiceException))),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
