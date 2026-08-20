using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

public class SupportQueueServiceException : Xeption
{
    public SupportQueueServiceException(Xeption innerException)
        : base("Support queue service error occurred, contact support.", innerException)
    { }
}
