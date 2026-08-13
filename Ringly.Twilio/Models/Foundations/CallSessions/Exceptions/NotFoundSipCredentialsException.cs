using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class NotFoundSipCredentialsException : Xeption
{
    public NotFoundSipCredentialsException(Guid clientId)
        : base($"No SIP credentials found for client with id: {clientId}.")
    { }
}
