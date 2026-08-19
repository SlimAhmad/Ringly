using Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService
{
    private static void ValidateAgentRequest(string channelId, string agentAppName)
    {
        if (string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(agentAppName))
        {
            var invalidAgentRequestException = new InvalidAgentRequestException();

            if (string.IsNullOrWhiteSpace(channelId))
            {
                invalidAgentRequestException.UpsertDataList(key: nameof(channelId), value: "Value is required");
            }

            if (string.IsNullOrWhiteSpace(agentAppName))
            {
                invalidAgentRequestException.UpsertDataList(key: nameof(agentAppName), value: "Value is required");
            }

            invalidAgentRequestException.ThrowIfContainsErrors();
        }
    }

    private static void ValidateAgentAppName(string agentAppName)
    {
        if (string.IsNullOrWhiteSpace(agentAppName))
        {
            var invalidAgentRequestException = new InvalidAgentRequestException();

            invalidAgentRequestException.UpsertDataList(
                key: nameof(agentAppName),
                value: "Value is required");

            invalidAgentRequestException.ThrowIfContainsErrors();
        }
    }
}
