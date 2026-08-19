using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

public class FailedStorageTelephonyIdentityDependencyException : Xeption
{
    public FailedStorageTelephonyIdentityDependencyException(Exception innerException)
        : base("Failed telephony identity storage dependency error occurred, contact support.", innerException)
    { }
}
