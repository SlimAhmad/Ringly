using Ringly.AiAgent.Abstractions.Models;
using Ringly.AiAgent.Twilio.Models.Exceptions;

namespace Ringly.AiAgent.Twilio.Services;

public partial class TwilioConversationRelayProvider
{
    private static void ValidateStartAiSessionRequest(string channelId, AiAgentConfig config)
    {
        if (config is null)
        {
            throw new NullAiAgentConfigException();
        }

        var invalidAiAgentSessionRequestException = new InvalidAiAgentSessionRequestException();

        if (string.IsNullOrWhiteSpace(channelId))
        {
            invalidAiAgentSessionRequestException.UpsertDataList(
                key: nameof(channelId),
                value: "Value is required");
        }

        invalidAiAgentSessionRequestException.ThrowIfContainsErrors();
    }

    private static void ValidateQueueName(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            var invalidAiAgentSessionRequestException = new InvalidAiAgentSessionRequestException();

            invalidAiAgentSessionRequestException.UpsertDataList(
                key: nameof(queueName),
                value: "Value is required");

            invalidAiAgentSessionRequestException.ThrowIfContainsErrors();
        }
    }
}
