using FluentAssertions;
using Moq;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.Twilio.Models;

namespace Ringly.CallCenter.Twilio.Tests.Unit.Services.Foundations.Queues;

public partial class TwilioCallCenterProviderTests
{
    [Fact]
    public async Task ShouldCreateQueueAsync()
    {
        // given
        QueueConfig inputQueueConfig = CreateRandomQueueConfig();
        TwilioTaskQueue returnedTaskQueue = CreateRandomTaskQueue();

        var expectedHoldingBridge = new HoldingBridge
        {
            BridgeId = returnedTaskQueue.Sid,
            QueueName = returnedTaskQueue.FriendlyName
        };

        this.twilioBrokerMock.Setup(broker =>
            broker.InsertTaskQueueAsync(inputQueueConfig.Name))
                .ReturnsAsync(returnedTaskQueue);

        // when
        HoldingBridge actualHoldingBridge =
            await this.twilioCallCenterProvider.CreateQueueAsync(inputQueueConfig);

        // then
        actualHoldingBridge.Should().BeEquivalentTo(expectedHoldingBridge);

        this.twilioBrokerMock.Verify(broker =>
            broker.InsertTaskQueueAsync(inputQueueConfig.Name),
                Times.Once);

        this.twilioBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
