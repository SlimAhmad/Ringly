using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Models;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService : ICallCenterProvider
{
    private const string HoldingBridgeType = "holding";

    private readonly IAsteriskBroker asteriskBroker;
    private readonly ILoggingBroker loggingBroker;

    public AsteriskCallCenterFoundationService(
        IAsteriskBroker asteriskBroker,
        ILoggingBroker loggingBroker)
    {
        this.asteriskBroker = asteriskBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<HoldingBridge> CreateQueueAsync(QueueConfig config) =>
    TryCatch(async () =>
    {
        ValidateQueueConfig(config);

        Bridge bridge = await this.asteriskBroker.InsertBridgeAsync(HoldingBridgeType);

        return new HoldingBridge
        {
            BridgeId = bridge.Id,
            QueueName = config.Name
        };
    });
}
