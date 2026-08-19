using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

public class FailedTelephonyDeviceServiceException : Xeption
{
    public FailedTelephonyDeviceServiceException(Exception innerException)
        : base("Failed telephony device service error occurred, contact support.", innerException)
    { }
}
