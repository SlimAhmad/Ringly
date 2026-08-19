using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

public class TelephonyIdentityDependencyException : Xeption
{
    public TelephonyIdentityDependencyException(Xeption innerException)
        : base("Telephony identity dependency error occurred, contact support.", innerException)
    { }
}
