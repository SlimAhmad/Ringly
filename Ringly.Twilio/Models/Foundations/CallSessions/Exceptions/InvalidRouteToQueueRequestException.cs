using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class InvalidRouteToQueueRequestException : Xeption
{
    public InvalidRouteToQueueRequestException()
        : base("Route to queue request is invalid.")
    { }
}
