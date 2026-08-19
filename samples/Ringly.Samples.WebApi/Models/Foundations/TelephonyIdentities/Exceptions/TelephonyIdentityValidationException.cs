using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

public class TelephonyIdentityValidationException : Xeption
{
    public TelephonyIdentityValidationException(Xeption innerException)
        : base("Telephony identity validation error occurred, fix errors and try again.", innerException)
    { }
}
