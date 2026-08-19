using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

public class TelephonyCallServiceException : Xeption
{
    public TelephonyCallServiceException(Xeption innerException)
        : base("Telephony call service error occurred, contact support.", innerException)
    { }
}
