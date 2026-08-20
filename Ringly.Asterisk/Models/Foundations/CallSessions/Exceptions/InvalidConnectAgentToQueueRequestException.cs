using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class InvalidConnectAgentToQueueRequestException : Xeption
{
    public InvalidConnectAgentToQueueRequestException()
        : base("Connect agent to queue request is invalid.")
    { }
}
