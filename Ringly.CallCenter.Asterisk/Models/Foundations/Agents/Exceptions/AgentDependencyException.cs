using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

public class AgentDependencyException : Xeption
{
    public AgentDependencyException(Xeption innerException)
        : base("Agent dependency error occurred, contact support.", innerException)
    { }
}
