using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class NotFoundQueueException : Xeption
{
    public NotFoundQueueException(string queueName)
        : base($"No queue found with name: {queueName}.")
    { }
}
