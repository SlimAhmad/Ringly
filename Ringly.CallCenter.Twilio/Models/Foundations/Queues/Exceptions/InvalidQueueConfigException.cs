using Xeptions;

namespace Ringly.CallCenter.Twilio.Models.Foundations.Queues.Exceptions;

public class InvalidQueueConfigException : Xeption
{
    public InvalidQueueConfigException()
        : base("Queue config is invalid.")
    { }
}
