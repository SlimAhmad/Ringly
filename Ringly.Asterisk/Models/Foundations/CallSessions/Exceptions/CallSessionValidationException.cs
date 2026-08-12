using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class CallSessionValidationException : Xeption
{
    public CallSessionValidationException(Xeption innerException)
        : base("Call session validation error occurred, fix errors and try again.", innerException)
    { }
}
