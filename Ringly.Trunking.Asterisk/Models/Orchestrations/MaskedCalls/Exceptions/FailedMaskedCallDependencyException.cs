using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Orchestrations.MaskedCalls.Exceptions;

public class FailedMaskedCallDependencyException : Xeption
{
    public FailedMaskedCallDependencyException(Exception innerException)
        : base("Failed masked call dependency error occurred, contact support.", innerException)
    { }
}
