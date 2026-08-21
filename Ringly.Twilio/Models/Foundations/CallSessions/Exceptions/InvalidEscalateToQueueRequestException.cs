using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class InvalidEscalateToQueueRequestException : Xeption
{
    public InvalidEscalateToQueueRequestException()
        : base("Escalate to queue request is invalid.")
    { }
}
