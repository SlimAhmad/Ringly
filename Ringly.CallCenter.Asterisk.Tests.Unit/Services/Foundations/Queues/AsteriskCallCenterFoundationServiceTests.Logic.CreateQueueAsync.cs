using FluentAssertions;
using Moq;
using Ringly.Asterisk.Models;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.CallCenter.Asterisk.Tests.Unit.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationServiceTests
{
    [Fact]
    public async Task ShouldCreateQueueAsync()
    {
        // given
        QueueConfig inputQueueConfig = CreateRandomQueueConfig();
        Bridge returnedBridge = CreateRandomBridge();

        var expectedHoldingBridge = new HoldingBridge
        {
            BridgeId = returnedBridge.Id,
            QueueName = inputQueueConfig.Name
        };

        this.asteriskBrokerMock.Setup(broker =>
            broker.InsertBridgeAsync("holding"))
                .ReturnsAsync(returnedBridge);

        // when
        HoldingBridge actualHoldingBridge =
            await this.asteriskCallCenterFoundationService.CreateQueueAsync(inputQueueConfig);

        // then
        actualHoldingBridge.Should().BeEquivalentTo(expectedHoldingBridge);

        this.asteriskBrokerMock.Verify(broker =>
            broker.InsertBridgeAsync("holding"),
                Times.Once);

        this.queueRegistryMock.Verify(registry =>
            registry.RegisterAsync(It.Is<HoldingBridge>(holdingBridge =>
                holdingBridge.BridgeId == expectedHoldingBridge.BridgeId &&
                holdingBridge.QueueName == expectedHoldingBridge.QueueName)),
                    Times.Once);

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.queueRegistryMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
