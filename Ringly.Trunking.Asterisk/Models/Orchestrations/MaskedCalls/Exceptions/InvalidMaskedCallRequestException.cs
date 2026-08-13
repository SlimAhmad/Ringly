using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Orchestrations.MaskedCalls.Exceptions;

public class InvalidMaskedCallRequestException : Xeption
{
    public InvalidMaskedCallRequestException()
        : base("Masked call request is invalid.")
    { }
}
