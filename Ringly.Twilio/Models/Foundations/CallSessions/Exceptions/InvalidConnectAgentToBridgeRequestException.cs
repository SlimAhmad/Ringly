using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class InvalidConnectAgentToBridgeRequestException : Xeption
{
    public InvalidConnectAgentToBridgeRequestException()
        : base("Connect agent to bridge request is invalid.")
    { }
}
