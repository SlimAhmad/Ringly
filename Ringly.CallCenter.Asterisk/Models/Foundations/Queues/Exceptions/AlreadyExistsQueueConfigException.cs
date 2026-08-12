using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;

public class AlreadyExistsQueueConfigException : Xeption
{
    public AlreadyExistsQueueConfigException(Exception innerException)
        : base("Queue config already exists.", innerException)
    { }
}
