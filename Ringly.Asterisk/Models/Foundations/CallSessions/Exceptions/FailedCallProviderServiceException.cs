using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class FailedCallProviderServiceException : Xeption
{
    public FailedCallProviderServiceException(Exception innerException)
        : base("Failed call provider service error occurred, contact support.", innerException)
    { }
}
