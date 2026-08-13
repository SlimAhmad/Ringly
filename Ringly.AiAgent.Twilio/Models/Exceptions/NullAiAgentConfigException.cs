using Xeptions;

namespace Ringly.AiAgent.Twilio.Models.Exceptions;

public class NullAiAgentConfigException : Xeption
{
    public NullAiAgentConfigException()
        : base("Ai agent config is null.")
    { }
}
