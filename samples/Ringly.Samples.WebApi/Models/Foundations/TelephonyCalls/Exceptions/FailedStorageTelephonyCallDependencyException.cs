using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

public class FailedStorageTelephonyCallDependencyException : Xeption
{
    public FailedStorageTelephonyCallDependencyException(Exception innerException)
        : base("Failed telephony call storage dependency error occurred, contact support.", innerException)
    { }
}
