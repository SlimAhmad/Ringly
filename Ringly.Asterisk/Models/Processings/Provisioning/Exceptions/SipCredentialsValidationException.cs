using Xeptions;

namespace Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

public class SipCredentialsValidationException : Xeption
{
    public SipCredentialsValidationException(Xeption innerException)
        : base("SIP credentials validation error occurred, fix errors and try again.", innerException)
    { }
}
