using Xeptions;

namespace Ringly.AiAgent.Twilio.Models.Exceptions;

public class AiAgentSessionNotFoundException : Xeption
{
    public AiAgentSessionNotFoundException(Guid aiSessionId)
        : base($"Could not find ai agent session with id: {aiSessionId}.")
    { }
}
