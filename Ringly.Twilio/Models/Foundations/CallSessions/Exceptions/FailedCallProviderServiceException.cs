using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class FailedCallProviderServiceException : Xeption
{
    public FailedCallProviderServiceException(Exception innerException)
        : base("Failed call provider service error occurred, contact support.", innerException)
    { }
}
