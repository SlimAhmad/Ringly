using Xeptions;

namespace Ringly.CallCenter.Twilio.Models.Foundations.Transfers.Exceptions;

public class FailedTransferServiceException : Xeption
{
    public FailedTransferServiceException(Exception innerException)
        : base("Failed transfer service error occurred, contact support.", innerException)
    { }
}
