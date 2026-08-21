using Ringly.Asterisk.Brokers;
using Ringly.Samples.WebApi.Brokers.Storages;
using Ringly.Samples.WebApi.Models.Foundations.SupportQueues;
using Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

namespace Ringly.Samples.WebApi.Services.Foundations.SupportQueues;

public partial class SupportQueueService : ISupportQueueService
{
    private readonly IStorageBroker storageBroker;
    private readonly ILoggingBroker loggingBroker;

    public SupportQueueService(IStorageBroker storageBroker, ILoggingBroker loggingBroker)
    {
        this.storageBroker = storageBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<SupportQueue> AddSupportQueueAsync(SupportQueue supportQueue) =>
    TryCatch(async () =>
    {
        ValidateSupportQueueOnAdd(supportQueue);

        return await this.storageBroker.InsertSupportQueueAsync(supportQueue);
    });

    public ValueTask<IQueryable<SupportQueue>> RetrieveAllSupportQueuesAsync() =>
    TryCatch(async () => await this.storageBroker.SelectAllSupportQueuesAsync());

    public ValueTask<SupportQueue> RetrieveSupportQueueByIdAsync(Guid supportQueueId) =>
    TryCatch(async () =>
    {
        ValidateSupportQueueId(supportQueueId);

        SupportQueue? maybeSupportQueue =
            await this.storageBroker.SelectSupportQueueByIdAsync(supportQueueId);

        ValidateStorageSupportQueueExists(maybeSupportQueue, supportQueueId);

        return maybeSupportQueue!;
    });

    public ValueTask<SupportQueue?> RetrieveSupportQueueByNameAsync(string queueName) =>
    TryCatchNullable(async () =>
    {
        ValidateQueueName(queueName);

        return await this.storageBroker.SelectSupportQueueByNameAsync(queueName);
    });

    public ValueTask<SupportQueue> ModifySupportQueueAsync(SupportQueue supportQueue) =>
    TryCatch(async () =>
    {
        ValidateSupportQueueOnModify(supportQueue);

        SupportQueue? maybeSupportQueue =
            await this.storageBroker.SelectSupportQueueByIdAsync(supportQueue.Id);

        ValidateStorageSupportQueueExists(maybeSupportQueue, supportQueue.Id);

        // See RecordingService.ModifyRecordingAsync's own comment — SelectSupportQueueByIdAsync
        // above already tracks an instance with this Id (EF's FindAsync); updating the
        // caller-supplied `supportQueue` instead, a different object with the same key, throws
        // "cannot be tracked because another instance with the same key value... is already
        // being tracked." Copying onto the already-tracked instance avoids the conflict.
        maybeSupportQueue!.QueueName = supportQueue.QueueName;
        maybeSupportQueue.BridgeId = supportQueue.BridgeId;
        maybeSupportQueue.MusicOnHoldClass = supportQueue.MusicOnHoldClass;

        return await this.storageBroker.UpdateSupportQueueAsync(maybeSupportQueue);
    });

    public ValueTask<SupportQueue> RemoveSupportQueueByIdAsync(Guid supportQueueId) =>
    TryCatch(async () =>
    {
        ValidateSupportQueueId(supportQueueId);

        SupportQueue? maybeSupportQueue =
            await this.storageBroker.SelectSupportQueueByIdAsync(supportQueueId);

        ValidateStorageSupportQueueExists(maybeSupportQueue, supportQueueId);

        return await this.storageBroker.DeleteSupportQueueAsync(maybeSupportQueue!);
    });
}
