using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

public class TelephonyCallDependencyException : Xeption
{
    public TelephonyCallDependencyException(Xeption innerException)
        : base("Telephony call dependency error occurred, contact support.", innerException)
    { }
}
