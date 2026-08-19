using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

public class AgentServiceException : Xeption
{
    public AgentServiceException(Xeption innerException)
        : base("Agent service error occurred, contact support.", innerException)
    { }
}
