using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

public class FailedSupportQueueServiceException : Xeption
{
    public FailedSupportQueueServiceException(Exception innerException)
        : base("Failed support queue service error occurred, contact support.", innerException)
    { }
}
