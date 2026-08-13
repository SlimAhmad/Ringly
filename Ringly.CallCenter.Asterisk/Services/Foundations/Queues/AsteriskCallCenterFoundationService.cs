using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Models;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService : ICallCenterProvider
{
    private const string HoldingBridgeType = "holding";

    private readonly IAsteriskBroker asteriskBroker;
    private readonly IQueueRegistry queueRegistry;
    private readonly ILoggingBroker loggingBroker;

    public AsteriskCallCenterFoundationService(
        IAsteriskBroker asteriskBroker,
        IQueueRegistry queueRegistry,
        ILoggingBroker loggingBroker)
    {
        this.asteriskBroker = asteriskBroker;
        this.queueRegistry = queueRegistry;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<HoldingBridge> CreateQueueAsync(QueueConfig config) =>
    TryCatch(async () =>
    {
        ValidateQueueConfig(config);

        Bridge bridge = await this.asteriskBroker.InsertBridgeAsync(HoldingBridgeType);

        var holdingBridge = new HoldingBridge
        {
            BridgeId = bridge.Id,
            QueueName = config.Name
        };

        // Registered so RouteToQueueAsync (§3.1a) can find this queue by name later — the
        // Asterisk broker has no native way to look up a bridge by an app-level name.
        await this.queueRegistry.RegisterAsync(holdingBridge);

        return holdingBridge;
    });
}
