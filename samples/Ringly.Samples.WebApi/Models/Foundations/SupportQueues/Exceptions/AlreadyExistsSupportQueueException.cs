using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

public class AlreadyExistsSupportQueueException : Xeption
{
    public AlreadyExistsSupportQueueException(Exception innerException)
        : base("Support queue already exists.", innerException)
    { }
}
