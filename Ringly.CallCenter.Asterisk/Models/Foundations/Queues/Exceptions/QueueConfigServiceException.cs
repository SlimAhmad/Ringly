using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;

public class QueueConfigServiceException : Xeption
{
    public QueueConfigServiceException(Xeption innerException)
        : base("Queue config service error occurred, contact support.", innerException)
    { }
}
