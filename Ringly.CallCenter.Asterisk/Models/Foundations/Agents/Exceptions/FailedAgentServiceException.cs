using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

public class FailedAgentServiceException : Xeption
{
    public FailedAgentServiceException(Exception innerException)
        : base("Failed agent service error occurred, contact support.", innerException)
    { }
}
