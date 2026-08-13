using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class InvalidCallParticipantException : Xeption
{
    public InvalidCallParticipantException()
        : base("Call participant is invalid.")
    { }
}
