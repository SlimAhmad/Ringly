using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

public class FailedAsteriskAgentDependencyException : Xeption
{
    public FailedAsteriskAgentDependencyException(Exception innerException)
        : base("Failed Asterisk agent dependency error occurred, contact support.", innerException)
    { }
}
