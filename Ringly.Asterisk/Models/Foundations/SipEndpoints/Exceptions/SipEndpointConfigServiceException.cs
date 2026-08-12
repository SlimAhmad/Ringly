using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class SipEndpointConfigServiceException : Xeption
{
    public SipEndpointConfigServiceException(Xeption innerException)
        : base("SIP endpoint config service error occurred, contact support.", innerException)
    { }
}
