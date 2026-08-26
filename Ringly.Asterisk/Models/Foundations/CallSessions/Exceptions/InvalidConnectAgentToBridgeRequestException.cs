using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class InvalidConnectAgentToBridgeRequestException : Xeption
{
    public InvalidConnectAgentToBridgeRequestException()
        : base("Connect agent to bridge request is invalid.")
    { }
}
