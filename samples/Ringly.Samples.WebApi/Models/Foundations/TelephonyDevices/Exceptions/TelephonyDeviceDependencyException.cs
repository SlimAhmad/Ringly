using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

public class TelephonyDeviceDependencyException : Xeption
{
    public TelephonyDeviceDependencyException(Xeption innerException)
        : base("Telephony device dependency error occurred, contact support.", innerException)
    { }
}
