using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

public class NullTelephonyDeviceException : Xeption
{
    public NullTelephonyDeviceException()
        : base("Telephony device is null.")
    { }
}
