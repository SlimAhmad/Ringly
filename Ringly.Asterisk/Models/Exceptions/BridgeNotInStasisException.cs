using Xeptions;

namespace Ringly.Asterisk.Models.Exceptions;

public class BridgeNotInStasisException : Xeption
{
    public BridgeNotInStasisException()
        : base("Bridge is not under Stasis application control.")
    { }
}
