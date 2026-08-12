using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;

public class InvalidQueueConfigException : Xeption
{
    public InvalidQueueConfigException()
        : base("Queue config is invalid.")
    { }
}
