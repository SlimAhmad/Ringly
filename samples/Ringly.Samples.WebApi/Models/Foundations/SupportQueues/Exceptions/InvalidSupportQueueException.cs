using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

public class InvalidSupportQueueException : Xeption
{
    public InvalidSupportQueueException()
        : base("Support queue is invalid.")
    { }
}
