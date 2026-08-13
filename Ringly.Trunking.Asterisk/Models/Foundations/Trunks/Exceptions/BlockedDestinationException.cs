using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

public class BlockedDestinationException : Xeption
{
    public BlockedDestinationException(string phoneNumber)
        : base($"Destination not allowed by trunk configuration: {phoneNumber}")
    { }
}
