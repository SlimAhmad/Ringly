using Xeptions;

namespace Ringly.AiAgent.Twilio.Models.Exceptions;

public class AiAgentSessionValidationException : Xeption
{
    public AiAgentSessionValidationException(Xeption innerException)
        : base("Ai agent session validation error occurred, fix errors and try again.", innerException)
    { }
}
