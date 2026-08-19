using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

public class TelephonyDeviceServiceException : Xeption
{
    public TelephonyDeviceServiceException(Xeption innerException)
        : base("Telephony device service error occurred, contact support.", innerException)
    { }
}
