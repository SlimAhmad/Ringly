using Xeptions;

namespace Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

public class FailedTwilioCallProviderDependencyException : Xeption
{
    public FailedTwilioCallProviderDependencyException(Exception innerException)
        : base("Failed Twilio call provider dependency error occurred, contact support.", innerException)
    { }
}
