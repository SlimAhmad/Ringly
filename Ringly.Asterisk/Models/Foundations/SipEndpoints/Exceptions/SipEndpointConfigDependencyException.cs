using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class SipEndpointConfigDependencyException : Xeption
{
    public SipEndpointConfigDependencyException(Xeption innerException)
        : base("SIP endpoint config dependency error occurred, contact support.", innerException)
    { }
}
