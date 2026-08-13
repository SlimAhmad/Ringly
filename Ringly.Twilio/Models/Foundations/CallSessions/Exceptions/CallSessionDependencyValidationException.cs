using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class CallSessionDependencyValidationException : Xeption
{
    public CallSessionDependencyValidationException(Xeption innerException)
        : base("Call session dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
