using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

public class TelephonyCallDependencyValidationException : Xeption
{
    public TelephonyCallDependencyValidationException(Xeption innerException)
        : base("Telephony call dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
