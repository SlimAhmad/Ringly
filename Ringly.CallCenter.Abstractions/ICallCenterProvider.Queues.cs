using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.CallCenter.Abstractions;

public partial interface ICallCenterProvider
{
    ValueTask<HoldingBridge> CreateQueueAsync(QueueConfig config);
}
