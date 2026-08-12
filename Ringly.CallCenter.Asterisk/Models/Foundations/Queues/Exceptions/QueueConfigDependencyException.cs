using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;

public class QueueConfigDependencyException : Xeption
{
    public QueueConfigDependencyException(Xeption innerException)
        : base("Queue config dependency error occurred, contact support.", innerException)
    { }
}
