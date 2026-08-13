using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

public class SipTrunkDependencyException : Xeption
{
    public SipTrunkDependencyException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
