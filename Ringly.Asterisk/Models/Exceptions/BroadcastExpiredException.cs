using Xeptions;

namespace Ringly.Asterisk.Models.Exceptions;

public class BroadcastExpiredException : Xeption
{
    public BroadcastExpiredException()
        : base("Call broadcast claim window has expired.")
    { }
}
