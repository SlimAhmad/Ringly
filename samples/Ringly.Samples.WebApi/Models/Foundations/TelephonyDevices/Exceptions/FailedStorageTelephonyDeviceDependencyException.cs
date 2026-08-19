using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

public class FailedStorageTelephonyDeviceDependencyException : Xeption
{
    public FailedStorageTelephonyDeviceDependencyException(Exception innerException)
        : base("Failed telephony device storage dependency error occurred, contact support.", innerException)
    { }
}
