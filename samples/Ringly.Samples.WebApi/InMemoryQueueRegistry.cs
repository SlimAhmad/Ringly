using System.Collections.Concurrent;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.Samples.WebApi;

// Ringly needs somewhere to look up a queue's HoldingBridge by name, but doesn't own storage
// for it (see docs/call-center.md) — a real app would back this with a database.
public class InMemoryQueueRegistry : IQueueRegistry
{
    private readonly ConcurrentDictionary<string, HoldingBridge> queues = new();

    public ValueTask<HoldingBridge?> RetrieveByNameAsync(string queueName) =>
        ValueTask.FromResult(this.queues.GetValueOrDefault(queueName));

    public ValueTask RegisterAsync(HoldingBridge holdingBridge)
    {
        this.queues[holdingBridge.QueueName] = holdingBridge;
        return ValueTask.CompletedTask;
    }
}
