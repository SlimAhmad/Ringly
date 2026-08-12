using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class CallProviderDependencyException : Xeption
{
    public CallProviderDependencyException(Xeption innerException)
        : base("Call provider dependency error occurred, contact support.", innerException)
    { }
}
