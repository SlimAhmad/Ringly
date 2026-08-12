using Xeptions;

namespace Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

public class DuplicateExtensionException : Xeption
{
    public DuplicateExtensionException(Exception innerException)
        : base("Extension already exists.", innerException)
    { }
}
