using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Orchestrations.MaskedCalls.Exceptions;

public class MaskedCallDependencyException : Xeption
{
    public MaskedCallDependencyException(Xeption innerException)
        : base("Masked call dependency error occurred, contact support.", innerException)
    { }
}
