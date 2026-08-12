using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;

public class QueueConfigDependencyValidationException : Xeption
{
    public QueueConfigDependencyValidationException(Xeption innerException)
        : base("Queue config dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
