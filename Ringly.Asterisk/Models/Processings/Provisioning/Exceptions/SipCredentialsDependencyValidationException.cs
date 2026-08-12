using Xeptions;

namespace Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

public class SipCredentialsDependencyValidationException : Xeption
{
    public SipCredentialsDependencyValidationException(Xeption innerException)
        : base("SIP credentials dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
