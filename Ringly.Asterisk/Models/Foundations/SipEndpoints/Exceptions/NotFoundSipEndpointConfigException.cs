using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class NotFoundSipEndpointConfigException : Xeption
{
    public NotFoundSipEndpointConfigException(Exception innerException)
        : base("SIP endpoint config resource not found.", innerException)
    { }
}
