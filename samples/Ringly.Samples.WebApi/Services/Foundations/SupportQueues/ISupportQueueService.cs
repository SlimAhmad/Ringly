using Ringly.Samples.WebApi.Models.Foundations.SupportQueues;

namespace Ringly.Samples.WebApi.Services.Foundations.SupportQueues;

public interface ISupportQueueService
{
    ValueTask<SupportQueue> AddSupportQueueAsync(SupportQueue supportQueue);
    ValueTask<IQueryable<SupportQueue>> RetrieveAllSupportQueuesAsync();
    ValueTask<SupportQueue> RetrieveSupportQueueByIdAsync(Guid supportQueueId);
    ValueTask<SupportQueue?> RetrieveSupportQueueByNameAsync(string queueName);
    ValueTask<SupportQueue> ModifySupportQueueAsync(SupportQueue supportQueue);
    ValueTask<SupportQueue> RemoveSupportQueueByIdAsync(Guid supportQueueId);
}
