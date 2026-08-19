using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

public class FailedTelephonyIdentityServiceException : Xeption
{
    public FailedTelephonyIdentityServiceException(Exception innerException)
        : base("Failed telephony identity service error occurred, contact support.", innerException)
    { }
}
