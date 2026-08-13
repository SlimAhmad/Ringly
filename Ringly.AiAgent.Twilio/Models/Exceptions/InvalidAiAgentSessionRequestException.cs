using Xeptions;

namespace Ringly.AiAgent.Twilio.Models.Exceptions;

public class InvalidAiAgentSessionRequestException : Xeption
{
    public InvalidAiAgentSessionRequestException()
        : base("Ai agent session request is invalid.")
    { }
}
