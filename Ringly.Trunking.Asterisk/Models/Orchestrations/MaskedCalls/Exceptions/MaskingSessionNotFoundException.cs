using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Orchestrations.MaskedCalls.Exceptions;

public class MaskingSessionNotFoundException : Xeption
{
    public MaskingSessionNotFoundException(string dialedNumber)
        : base($"No active masking session found for number: {dialedNumber}")
    { }
}
