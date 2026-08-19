using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

public class FailedTelephonyCallServiceException : Xeption
{
    public FailedTelephonyCallServiceException(Exception innerException)
        : base("Failed telephony call service error occurred, contact support.", innerException)
    { }
}
