using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;

public class FailedAsteriskTransferDependencyException : Xeption
{
    public FailedAsteriskTransferDependencyException(Exception innerException)
        : base("Failed Asterisk transfer dependency error occurred, contact support.", innerException)
    { }
}
