using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

public class TelephonyDeviceDependencyValidationException : Xeption
{
    public TelephonyDeviceDependencyValidationException(Xeption innerException)
        : base("Telephony device dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
