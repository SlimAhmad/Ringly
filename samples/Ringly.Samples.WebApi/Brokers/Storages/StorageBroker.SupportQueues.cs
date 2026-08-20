using Microsoft.EntityFrameworkCore;
using Ringly.Samples.WebApi.Models.Foundations.SupportQueues;

namespace Ringly.Samples.WebApi.Brokers.Storages;

public partial class StorageBroker
{
    public DbSet<SupportQueue> SupportQueues { get; set; } = null!;

    public async ValueTask<SupportQueue> InsertSupportQueueAsync(SupportQueue supportQueue) =>
        await this.InsertAsync(supportQueue);

    public async ValueTask<IQueryable<SupportQueue>> SelectAllSupportQueuesAsync() =>
        await this.SelectAllAsync<SupportQueue>();

    public async ValueTask<SupportQueue> SelectSupportQueueByIdAsync(Guid supportQueueId) =>
        await this.SelectAsync<SupportQueue>(supportQueueId);

    public async ValueTask<SupportQueue?> SelectSupportQueueByNameAsync(string queueName)
    {
        IQueryable<SupportQueue> supportQueues = await this.SelectAllAsync<SupportQueue>();

        return supportQueues.FirstOrDefault(supportQueue => supportQueue.QueueName == queueName);
    }

    public async ValueTask<SupportQueue> UpdateSupportQueueAsync(SupportQueue supportQueue) =>
        await this.UpdateAsync(supportQueue);

    public async ValueTask<SupportQueue> DeleteSupportQueueAsync(SupportQueue supportQueue) =>
        await this.DeleteAsync(supportQueue);

    private void ConfigureSupportQueues(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupportQueue>(builder =>
        {
            builder.Property(supportQueue => supportQueue.Id).IsRequired();
            builder.Property(supportQueue => supportQueue.QueueName).HasMaxLength(255).IsRequired();
            builder.Property(supportQueue => supportQueue.BridgeId).HasMaxLength(255).IsRequired();
            builder.Property(supportQueue => supportQueue.MusicOnHoldClass).HasMaxLength(255);

            builder.HasIndex(supportQueue => supportQueue.QueueName).IsUnique();
        });
    }
}
