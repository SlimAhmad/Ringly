using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

public class SupportQueueValidationException : Xeption
{
    public SupportQueueValidationException(Xeption innerException)
        : base("Support queue validation error occurred, fix errors and try again.", innerException)
    { }
}
