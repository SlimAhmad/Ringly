using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

public class SipTrunkValidationException : Xeption
{
    public SipTrunkValidationException(Xeption innerException)
        : base("SIP trunk validation error occurred, fix errors and try again.", innerException)
    { }
}
