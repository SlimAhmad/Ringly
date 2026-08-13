using Xeptions;

namespace Ringly.AiAgent.Twilio.Models.Exceptions;

public class AiAgentSessionServiceException : Xeption
{
    public AiAgentSessionServiceException(Xeption innerException)
        : base("Ai agent session service error occurred, contact support.", innerException)
    { }
}
