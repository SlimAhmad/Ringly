using Ringly.Samples.WebApi.Models.Foundations.SupportQueues;

namespace Ringly.Samples.WebApi.Brokers.Storages;

public partial interface IStorageBroker
{
    ValueTask<SupportQueue> InsertSupportQueueAsync(SupportQueue supportQueue);
    ValueTask<IQueryable<SupportQueue>> SelectAllSupportQueuesAsync();
    ValueTask<SupportQueue> SelectSupportQueueByIdAsync(Guid supportQueueId);
    ValueTask<SupportQueue?> SelectSupportQueueByNameAsync(string queueName);
    ValueTask<SupportQueue> UpdateSupportQueueAsync(SupportQueue supportQueue);
    ValueTask<SupportQueue> DeleteSupportQueueAsync(SupportQueue supportQueue);
}
