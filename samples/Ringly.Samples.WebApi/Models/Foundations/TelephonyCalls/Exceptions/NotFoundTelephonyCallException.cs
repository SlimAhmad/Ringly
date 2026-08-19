using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

public class NotFoundTelephonyCallException : Xeption
{
    public NotFoundTelephonyCallException(Guid telephonyCallId)
        : base($"Could not find telephony call with id: {telephonyCallId}.")
    { }
}
