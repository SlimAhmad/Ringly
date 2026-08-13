using Xeptions;

namespace Ringly.CallCenter.Twilio.Models.Foundations.Queues.Exceptions;

public class NullQueueConfigException : Xeption
{
    public NullQueueConfigException()
        : base("Queue config is null.")
    { }
}
