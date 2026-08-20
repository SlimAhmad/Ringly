using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

public class NotFoundSupportQueueException : Xeption
{
    public NotFoundSupportQueueException(Guid supportQueueId)
        : base($"Could not find support queue with id: {supportQueueId}.")
    { }

    public NotFoundSupportQueueException(string queueName)
        : base($"Could not find support queue with name: {queueName}.")
    { }
}
