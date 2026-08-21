using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
{
    private static void ValidateEscalateToQueueRequest(string channelId, string queueName)
    {
        var invalidEscalateToQueueRequestException = new InvalidEscalateToQueueRequestException();

        if (string.IsNullOrWhiteSpace(channelId))
        {
            invalidEscalateToQueueRequestException.UpsertDataList(
                key: nameof(channelId),
                value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(queueName))
        {
            invalidEscalateToQueueRequestException.UpsertDataList(
                key: nameof(queueName),
                value: "Value is required");
        }

        invalidEscalateToQueueRequestException.ThrowIfContainsErrors();
    }
}
