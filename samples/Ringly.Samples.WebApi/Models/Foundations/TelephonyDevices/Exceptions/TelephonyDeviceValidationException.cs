using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

public class TelephonyDeviceValidationException : Xeption
{
    public TelephonyDeviceValidationException(Xeption innerException)
        : base("Telephony device validation error occurred, fix errors and try again.", innerException)
    { }
}
