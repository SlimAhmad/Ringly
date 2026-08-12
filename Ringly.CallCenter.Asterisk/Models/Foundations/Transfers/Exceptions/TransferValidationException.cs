using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;

public class TransferValidationException : Xeption
{
    public TransferValidationException(Xeption innerException)
        : base("Transfer validation error occurred, fix errors and try again.", innerException)
    { }
}
