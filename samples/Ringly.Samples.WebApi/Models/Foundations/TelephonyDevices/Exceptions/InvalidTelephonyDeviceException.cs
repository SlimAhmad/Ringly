using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

public class InvalidTelephonyDeviceException : Xeption
{
    public InvalidTelephonyDeviceException()
        : base("Telephony device is invalid.")
    { }
}
