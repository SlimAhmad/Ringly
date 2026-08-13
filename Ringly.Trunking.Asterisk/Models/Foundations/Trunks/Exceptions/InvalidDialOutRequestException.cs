using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

public class InvalidDialOutRequestException : Xeption
{
    public InvalidDialOutRequestException()
        : base("Dial-out request is invalid.")
    { }
}
