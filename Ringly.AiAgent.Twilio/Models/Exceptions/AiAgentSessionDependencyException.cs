using Xeptions;

namespace Ringly.AiAgent.Twilio.Models.Exceptions;

public class AiAgentSessionDependencyException : Xeption
{
    public AiAgentSessionDependencyException(Xeption innerException)
        : base("Ai agent session dependency error occurred, contact support.", innerException)
    { }
}
