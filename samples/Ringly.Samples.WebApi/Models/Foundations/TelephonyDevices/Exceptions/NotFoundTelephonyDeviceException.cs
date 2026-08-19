using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

public class NotFoundTelephonyDeviceException : Xeption
{
    public NotFoundTelephonyDeviceException(Guid telephonyDeviceId)
        : base($"Could not find telephony device with id: {telephonyDeviceId}.")
    { }
}
