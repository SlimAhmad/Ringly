using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

public class AgentDependencyValidationException : Xeption
{
    public AgentDependencyValidationException(Xeption innerException)
        : base("Agent dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
