using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

public class AgentValidationException : Xeption
{
    public AgentValidationException(Xeption innerException)
        : base("Agent validation error occurred, fix errors and try again.", innerException)
    { }
}
