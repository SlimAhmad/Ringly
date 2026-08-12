using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;

public class InvalidTransferProgressRequestException : Xeption
{
    public InvalidTransferProgressRequestException()
        : base("Transfer progress request is invalid.")
    { }
}
