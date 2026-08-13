using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

public class FailedSipTrunkServiceException : Xeption
{
    public FailedSipTrunkServiceException(Exception innerException)
        : base("Failed SIP trunk service error occurred, contact support.", innerException)
    { }
}
