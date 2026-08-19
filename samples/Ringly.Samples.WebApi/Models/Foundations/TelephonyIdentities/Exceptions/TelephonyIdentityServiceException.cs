using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

public class TelephonyIdentityServiceException : Xeption
{
    public TelephonyIdentityServiceException(Xeption innerException)
        : base("Telephony identity service error occurred, contact support.", innerException)
    { }
}
