using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class SipEndpointConfigDependencyValidationException : Xeption
{
    public SipEndpointConfigDependencyValidationException(Xeption innerException)
        : base("SIP endpoint config dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
