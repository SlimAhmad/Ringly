using Xeptions;

namespace Ringly.Asterisk.Models.Exceptions;

public class AsteriskDependencyException : Xeption
{
    public AsteriskDependencyException(Xeption innerException)
        : base("Asterisk dependency error occurred, contact support.", innerException)
    { }
}
