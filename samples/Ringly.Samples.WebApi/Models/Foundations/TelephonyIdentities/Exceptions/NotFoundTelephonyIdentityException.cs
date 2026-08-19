using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities.Exceptions;

public class NotFoundTelephonyIdentityException : Xeption
{
    public NotFoundTelephonyIdentityException(Guid telephonyIdentityId)
        : base($"Could not find telephony identity with id: {telephonyIdentityId}.")
    { }
}
