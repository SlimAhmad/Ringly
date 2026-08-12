using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class InvalidSipEndpointConfigException : Xeption
{
    public InvalidSipEndpointConfigException()
        : base("SIP endpoint config is invalid.")
    { }
}
