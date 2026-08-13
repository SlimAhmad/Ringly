using Xeptions;

namespace Ringly.AiAgent.Twilio.Models.Exceptions;

public class FailedAiAgentServiceException : Xeption
{
    public FailedAiAgentServiceException(Exception innerException)
        : base("Failed ai agent service error occurred, contact support.", innerException)
    { }
}
