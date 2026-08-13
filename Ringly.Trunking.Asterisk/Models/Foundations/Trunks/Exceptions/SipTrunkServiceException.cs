using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

public class SipTrunkServiceException : Xeption
{
    public SipTrunkServiceException(Xeption innerException)
        : base("SIP trunk service error occurred, contact support.", innerException)
    { }
}
