using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class NullSipEndpointConfigException : Xeption
{
    public NullSipEndpointConfigException()
        : base("SIP endpoint config is null.")
    { }
}
