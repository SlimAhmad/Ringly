using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models.Orchestrations.MaskedCalls.Exceptions;

namespace Ringly.Trunking.Asterisk.Services.Orchestrations.MaskedCalls;

public partial class MaskedCallOrchestrationService
{
    private static void ValidateTrunkCallEvent(TrunkCallEvent trunkEvent)
    {
        if (trunkEvent is null)
        {
            throw new InvalidMaskedCallRequestException();
        }

        var invalidMaskedCallRequestException = new InvalidMaskedCallRequestException();

        if (string.IsNullOrWhiteSpace(trunkEvent.DialedNumber))
        {
            invalidMaskedCallRequestException.UpsertDataList(
                key: nameof(TrunkCallEvent.DialedNumber),
                value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(trunkEvent.ChannelId))
        {
            invalidMaskedCallRequestException.UpsertDataList(
                key: nameof(TrunkCallEvent.ChannelId),
                value: "Value is required");
        }

        invalidMaskedCallRequestException.ThrowIfContainsErrors();
    }
}
