using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

public class FailedAsteriskCallProviderDependencyException : Xeption
{
    public FailedAsteriskCallProviderDependencyException(Exception innerException)
        : base("Failed Asterisk call provider dependency error occurred, contact support.", innerException)
    { }
}
