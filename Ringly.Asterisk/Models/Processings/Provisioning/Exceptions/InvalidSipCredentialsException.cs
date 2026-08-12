using Xeptions;

namespace Ringly.Asterisk.Models.Processings.Provisioning.Exceptions;

public class InvalidSipCredentialsException : Xeption
{
    public InvalidSipCredentialsException()
        : base("SIP credentials request is invalid.")
    { }
}
