using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class NullCallParticipantException : Xeption
{
    public NullCallParticipantException()
        : base("Call participant is null.")
    { }
}
