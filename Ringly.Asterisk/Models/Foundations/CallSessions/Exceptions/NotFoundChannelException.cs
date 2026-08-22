using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class NotFoundChannelException : Xeption
{
    public NotFoundChannelException(string callerNumber)
        : base($"No active channel found for caller number: {callerNumber}.")
    { }
}
