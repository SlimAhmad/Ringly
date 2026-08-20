using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

public class NullSupportQueueException : Xeption
{
    public NullSupportQueueException()
        : base("Support queue is null.")
    { }
}
