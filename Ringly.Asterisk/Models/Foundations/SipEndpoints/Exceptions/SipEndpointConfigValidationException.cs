using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class SipEndpointConfigValidationException : Xeption
{
    public SipEndpointConfigValidationException(Xeption innerException)
        : base("SIP endpoint config validation error occurred, fix errors and try again.", innerException)
    { }
}
