using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.SupportQueues.Exceptions;

public class FailedStorageSupportQueueDependencyException : Xeption
{
    public FailedStorageSupportQueueDependencyException(Exception innerException)
        : base("Failed support queue storage dependency error occurred, contact support.", innerException)
    { }
}
