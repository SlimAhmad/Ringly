using Xeptions;

namespace Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

public class SipCredentialsServiceException : Xeption
{
    public SipCredentialsServiceException(Xeption innerException)
        : base("SIP credentials service error occurred, contact support.", innerException)
    { }
}
