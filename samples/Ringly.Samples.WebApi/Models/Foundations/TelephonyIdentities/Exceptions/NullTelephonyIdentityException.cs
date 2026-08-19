using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

public class NullTelephonyIdentityException : Xeption
{
    public NullTelephonyIdentityException()
        : base("Telephony identity is null.")
    { }
}
