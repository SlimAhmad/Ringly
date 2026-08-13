using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

public class SipTrunkDependencyValidationException : Xeption
{
    public SipTrunkDependencyValidationException(Xeption innerException)
        : base("SIP trunk dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
