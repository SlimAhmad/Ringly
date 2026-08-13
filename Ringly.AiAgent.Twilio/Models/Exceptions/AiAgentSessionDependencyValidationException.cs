using Xeptions;

namespace Ringly.AiAgent.Twilio.Models.Exceptions;

public class AiAgentSessionDependencyValidationException : Xeption
{
    public AiAgentSessionDependencyValidationException(Xeption innerException)
        : base("Ai agent session dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
