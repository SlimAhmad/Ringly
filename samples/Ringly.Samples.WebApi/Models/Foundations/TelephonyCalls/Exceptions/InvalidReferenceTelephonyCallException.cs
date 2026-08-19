using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

// Thrown when CallerIdentityId or RecipientIdentityId doesn't reference a real TelephonyIdentity
// row — foreign key violation (EFxceptions.ForeignKeyConstraintConflictException), same pattern
// as TelephonyDevice's InvalidReferenceTelephonyDeviceException.
public class InvalidReferenceTelephonyCallException : Xeption
{
    public InvalidReferenceTelephonyCallException(Exception innerException)
        : base("Invalid telephony call reference error occurred.", innerException)
    { }
}
