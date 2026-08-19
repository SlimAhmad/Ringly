using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

public class TelephonyCallValidationException : Xeption
{
    public TelephonyCallValidationException(Xeption innerException)
        : base("Telephony call validation error occurred, fix errors and try again.", innerException)
    { }
}
