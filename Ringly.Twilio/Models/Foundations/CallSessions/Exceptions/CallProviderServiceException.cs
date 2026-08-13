using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class CallProviderServiceException : Xeption
{
    public CallProviderServiceException(Xeption innerException)
        : base("Call provider service error occurred, contact support.", innerException)
    { }
}
