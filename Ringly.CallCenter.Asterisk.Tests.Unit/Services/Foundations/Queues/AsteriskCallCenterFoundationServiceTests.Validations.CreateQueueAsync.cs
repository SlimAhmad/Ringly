using FluentAssertions;
using Moq;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnCreateQueueIfConfigIsNullAndLogItAsync()
    {
        // given
        QueueConfig nullQueueConfig = null!;
        var nullQueueConfigException = new NullQueueConfigException();

        var expectedQueueConfigValidationException =
            new QueueConfigValidationException(nullQueueConfigException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.asteriskCallCenterFoundationService.CreateQueueAsync(nullQueueConfig);

        QueueConfigValidationException actualException =
            await Assert.ThrowsAsync<QueueConfigValidationException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedQueueConfigValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnCreateQueueIfNameIsInvalidAndLogItAsync(string? invalidName)
    {
        // given
        QueueConfig invalidQueueConfig = CreateRandomQueueConfig();
        invalidQueueConfig.Name = invalidName!;

        var invalidQueueConfigException = new InvalidQueueConfigException();

        invalidQueueConfigException.UpsertDataList(
            key: nameof(QueueConfig.Name),
            value: "Value is required");

        var expectedQueueConfigValidationException =
            new QueueConfigValidationException(invalidQueueConfigException);

        // when
        ValueTask<HoldingBridge> createQueueTask =
            this.asteriskCallCenterFoundationService.CreateQueueAsync(invalidQueueConfig);

        QueueConfigValidationException actualException =
            await Assert.ThrowsAsync<QueueConfigValidationException>(createQueueTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedQueueConfigValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedQueueConfigValidationException))),
                Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
