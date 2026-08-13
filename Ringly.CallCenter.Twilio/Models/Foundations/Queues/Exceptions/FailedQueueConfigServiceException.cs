using Xeptions;

namespace Ringly.CallCenter.Twilio.Models.Foundations.Queues.Exceptions;

public class FailedQueueConfigServiceException : Xeption
{
    public FailedQueueConfigServiceException(Exception innerException)
        : base("Failed queue config service error occurred, contact support.", innerException)
    { }
}
