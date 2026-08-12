using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Queues.Exceptions;

public class FailedAsteriskQueueConfigDependencyException : Xeption
{
    public FailedAsteriskQueueConfigDependencyException(Exception innerException)
        : base("Failed Asterisk queue config dependency error occurred, contact support.", innerException)
    { }
}
