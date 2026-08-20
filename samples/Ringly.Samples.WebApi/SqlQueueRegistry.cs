using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.Samples.WebApi.Models.Foundations.SupportQueues;
using Ringly.Samples.WebApi.Services.Foundations.SupportQueues;

namespace Ringly.Samples.WebApi;

// The real, SQL-Server-backed IQueueRegistry implementation, replacing InMemoryQueueRegistry —
// same rationale as SqlSipCredentialsStore replacing InMemorySipCredentialsStore: queues
// registered via the UI/API need to survive a WebApi restart, not vanish the moment the process
// recycles. InMemoryQueueRegistry stays in the codebase as a zero-setup fallback, same precedent.
//
// Maps HoldingBridge (the library's thin BridgeId/QueueName contract) onto SupportQueue (this
// sample's richer storage row, which also tracks MusicOnHoldClass for the UI's own create form).
public class SqlQueueRegistry : IQueueRegistry
{
    private readonly ISupportQueueService supportQueueService;

    public SqlQueueRegistry(ISupportQueueService supportQueueService) =>
        this.supportQueueService = supportQueueService;

    public async ValueTask<HoldingBridge?> RetrieveByNameAsync(string queueName)
    {
        SupportQueue? supportQueue = await this.supportQueueService.RetrieveSupportQueueByNameAsync(queueName);

        return supportQueue is null
            ? null
            : new HoldingBridge { BridgeId = supportQueue.BridgeId, QueueName = supportQueue.QueueName };
    }

    public async ValueTask RegisterAsync(HoldingBridge holdingBridge) =>
        await this.supportQueueService.AddSupportQueueAsync(new SupportQueue
        {
            Id = Guid.NewGuid(),
            QueueName = holdingBridge.QueueName,
            BridgeId = holdingBridge.BridgeId
        });

    public async ValueTask<IReadOnlyList<HoldingBridge>> RetrieveAllAsync()
    {
        IQueryable<SupportQueue> supportQueues = await this.supportQueueService.RetrieveAllSupportQueuesAsync();

        return supportQueues
            .Select(supportQueue => new HoldingBridge
            {
                BridgeId = supportQueue.BridgeId,
                QueueName = supportQueue.QueueName
            })
            .ToList();
    }

    public async ValueTask RemoveAsync(string queueName)
    {
        SupportQueue? supportQueue = await this.supportQueueService.RetrieveSupportQueueByNameAsync(queueName);

        if (supportQueue is not null)
        {
            await this.supportQueueService.RemoveSupportQueueByIdAsync(supportQueue.Id);
        }
    }
}
