using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

public class AlreadyExistsTelephonyDeviceException : Xeption
{
    public AlreadyExistsTelephonyDeviceException(Exception innerException)
        : base("Telephony device already exists.", innerException)
    { }
}
