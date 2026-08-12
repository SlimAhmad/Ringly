using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class FailedAsteriskSipEndpointConfigDependencyException : Xeption
{
    public FailedAsteriskSipEndpointConfigDependencyException(Exception innerException)
        : base("Failed Asterisk SIP endpoint config dependency error occurred, contact support.", innerException)
    { }
}
