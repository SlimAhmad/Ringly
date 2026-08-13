using Xeptions;

namespace Ringly.CallCenter.Twilio.Models.Foundations.Transfers.Exceptions;

public class TransferServiceException : Xeption
{
    public TransferServiceException(Xeption innerException)
        : base("Transfer service error occurred, contact support.", innerException)
    { }
}
