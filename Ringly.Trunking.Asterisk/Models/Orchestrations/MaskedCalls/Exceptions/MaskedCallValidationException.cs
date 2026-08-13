using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Orchestrations.MaskedCalls.Exceptions;

public class MaskedCallValidationException : Xeption
{
    public MaskedCallValidationException(Xeption innerException)
        : base("Masked call validation error occurred, fix errors and try again.", innerException)
    { }
}
