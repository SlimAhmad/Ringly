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

        this.asteriskBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
