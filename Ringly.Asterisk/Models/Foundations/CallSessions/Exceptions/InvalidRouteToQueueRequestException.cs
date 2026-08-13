using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class InvalidRouteToQueueRequestException : Xeption
{
    public InvalidRouteToQueueRequestException()
        : base("Route to queue request is invalid.")
    { }
}
