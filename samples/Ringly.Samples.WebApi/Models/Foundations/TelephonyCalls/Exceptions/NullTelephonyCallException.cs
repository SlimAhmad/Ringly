using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

public class NullTelephonyCallException : Xeption
{
    public NullTelephonyCallException()
        : base("Telephony call is null.")
    { }
}
