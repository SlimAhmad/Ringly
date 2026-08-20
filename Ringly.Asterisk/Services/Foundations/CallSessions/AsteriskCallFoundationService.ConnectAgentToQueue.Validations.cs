using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
{
    private static void ValidateConnectAgentToQueueRequest(
        string bridgeId, string customerChannelId, string agentExtension)
    {
        var invalidConnectAgentToQueueRequestException = new InvalidConnectAgentToQueueRequestException();

        if (string.IsNullOrWhiteSpace(bridgeId))
        {
            invalidConnectAgentToQueueRequestException.UpsertDataList(
                key: nameof(bridgeId),
                value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(customerChannelId))
        {
            invalidConnectAgentToQueueRequestException.UpsertDataList(
                key: nameof(customerChannelId),
                value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(agentExtension))
        {
            invalidConnectAgentToQueueRequestException.UpsertDataList(
                key: nameof(agentExtension),
                value: "Value is required");
        }

        invalidConnectAgentToQueueRequestException.ThrowIfContainsErrors();
    }
}
