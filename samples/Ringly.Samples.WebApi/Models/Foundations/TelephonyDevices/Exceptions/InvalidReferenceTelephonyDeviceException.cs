using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices.Exceptions;

// Thrown when IdentityId doesn't reference a real TelephonyIdentity row — foreign key violation
// (EFxceptions.ForeignKeyConstraintConflictException), not a plain validation failure, since the
// Guid itself is well-formed; it just doesn't point at anything real in storage.
public class InvalidReferenceTelephonyDeviceException : Xeption
{
    public InvalidReferenceTelephonyDeviceException(Exception innerException)
        : base("Invalid telephony device reference error occurred.", innerException)
    { }
}
