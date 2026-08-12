using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;

public class TransferDependencyException : Xeption
{
    public TransferDependencyException(Xeption innerException)
        : base("Transfer dependency error occurred, contact support.", innerException)
    { }
}
