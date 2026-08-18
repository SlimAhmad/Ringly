using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class ExtensionNotFoundException : Xeption
{
    public ExtensionNotFoundException(Exception innerException)
        : base("Extension not found.", innerException)
    { }
}
