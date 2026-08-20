using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

public class SupportQueueDependencyValidationException : Xeption
{
    public SupportQueueDependencyValidationException(Xeption innerException)
        : base("Support queue dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
