using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;

public class TransferServiceException : Xeption
{
    public TransferServiceException(Xeption innerException)
        : base("Transfer service error occurred, contact support.", innerException)
    { }
}
