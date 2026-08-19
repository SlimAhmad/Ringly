using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

public class InvalidTelephonyIdentityException : Xeption
{
    public InvalidTelephonyIdentityException()
        : base("Telephony identity is invalid.")
    { }
}
