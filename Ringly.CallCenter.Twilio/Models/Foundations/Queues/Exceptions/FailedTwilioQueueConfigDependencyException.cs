using Xeptions;

namespace Ringly.CallCenter.Twilio.Models.Foundations.Queues.Exceptions;

public class FailedTwilioQueueConfigDependencyException : Xeption
{
    public FailedTwilioQueueConfigDependencyException(Exception innerException)
        : base("Failed Twilio queue config dependency error occurred, contact support.", innerException)
    { }
}
