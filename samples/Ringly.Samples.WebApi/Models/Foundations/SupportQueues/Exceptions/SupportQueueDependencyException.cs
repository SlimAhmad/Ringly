using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

public class SupportQueueDependencyException : Xeption
{
    public SupportQueueDependencyException(Xeption innerException)
        : base("Support queue dependency error occurred, contact support.", innerException)
    { }
}
