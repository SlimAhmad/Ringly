using Xeptions;

namespace Ringly.CallCenter.Twilio.Models.Foundations.Queues.Exceptions;

public class QueueConfigValidationException : Xeption
{
    public QueueConfigValidationException(Xeption innerException)
        : base("Queue config validation error occurred, fix errors and try again.", innerException)
    { }
}
