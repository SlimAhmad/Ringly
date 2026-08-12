using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class InvalidCallParticipantException : Xeption
{
    public InvalidCallParticipantException()
        : base("Call participant is invalid.")
    { }
}
