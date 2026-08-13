using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class NullCallParticipantException : Xeption
{
    public NullCallParticipantException()
        : base("Call participant is null.")
    { }
}
