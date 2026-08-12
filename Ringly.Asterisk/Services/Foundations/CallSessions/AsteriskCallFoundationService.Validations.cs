using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
{
    private static void ValidateCallParticipant(CallParticipant participant)
    {
        if (participant is null)
        {
            throw new NullCallParticipantException();
        }

        var invalidCallParticipantException = new InvalidCallParticipantException();

        if (string.IsNullOrWhiteSpace(participant.SipExtension))
        {
            invalidCallParticipantException.UpsertDataList(
                key: nameof(CallParticipant.SipExtension),
                value: "Value is required");
        }

        invalidCallParticipantException.ThrowIfContainsErrors();
    }
}
