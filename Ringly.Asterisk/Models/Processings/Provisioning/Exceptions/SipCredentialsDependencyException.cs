using Xeptions;

namespace Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

public class SipCredentialsDependencyException : Xeption
{
    public SipCredentialsDependencyException(Xeption innerException)
        : base("SIP credentials dependency error occurred, contact support.", innerException)
    { }
}
