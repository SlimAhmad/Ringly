using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class CallProviderServiceException : Xeption
{
    public CallProviderServiceException(Xeption innerException)
        : base("Call provider service error occurred, contact support.", innerException)
    { }
}
