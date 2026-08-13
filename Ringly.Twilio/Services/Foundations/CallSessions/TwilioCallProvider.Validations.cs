using Ringly.Abstractions.Models;
using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

public partial class TwilioCallProvider
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
