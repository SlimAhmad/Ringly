using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class NotFoundChannelException : Xeption
{
    public NotFoundChannelException(string callerNumber)
        : base($"No in-progress call found for caller number: {callerNumber}.")
    { }
}
