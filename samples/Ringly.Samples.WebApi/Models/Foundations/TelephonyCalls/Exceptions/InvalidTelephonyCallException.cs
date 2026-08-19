using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

public class InvalidTelephonyCallException : Xeption
{
    public InvalidTelephonyCallException()
        : base("Telephony call is invalid.")
    { }
}
