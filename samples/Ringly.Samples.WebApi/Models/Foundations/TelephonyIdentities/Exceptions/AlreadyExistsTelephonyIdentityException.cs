using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

public class AlreadyExistsTelephonyIdentityException : Xeption
{
    public AlreadyExistsTelephonyIdentityException(Exception innerException)
        : base("Telephony identity already exists.", innerException)
    { }
}
