using Xeptions;

namespace Ringly.AiAgent.Twilio.Models.Exceptions;

public class FailedTwilioAiAgentDependencyException : Xeption
{
    public FailedTwilioAiAgentDependencyException(Exception innerException)
        : base("Failed Twilio ai agent dependency error occurred, contact support.", innerException)
    { }
}
