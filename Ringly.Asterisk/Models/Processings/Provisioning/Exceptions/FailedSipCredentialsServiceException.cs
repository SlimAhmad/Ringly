using Xeptions;

namespace Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

public class FailedSipCredentialsServiceException : Xeption
{
    public FailedSipCredentialsServiceException(Exception innerException)
        : base("Failed SIP credentials service error occurred, contact support.", innerException)
    { }
}
