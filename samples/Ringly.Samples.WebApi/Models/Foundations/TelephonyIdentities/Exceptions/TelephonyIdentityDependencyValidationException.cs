using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

public class TelephonyIdentityDependencyValidationException : Xeption
{
    public TelephonyIdentityDependencyValidationException(Xeption innerException)
        : base("Telephony identity dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
