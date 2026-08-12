using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;

public class TransferDependencyValidationException : Xeption
{
    public TransferDependencyValidationException(Xeption innerException)
        : base("Transfer dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
