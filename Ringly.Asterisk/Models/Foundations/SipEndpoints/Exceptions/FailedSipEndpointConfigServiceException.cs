using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class FailedSipEndpointConfigServiceException : Xeption
{
    public FailedSipEndpointConfigServiceException(Exception innerException)
        : base("Failed SIP endpoint config service error occurred, contact support.", innerException)
    { }
}
