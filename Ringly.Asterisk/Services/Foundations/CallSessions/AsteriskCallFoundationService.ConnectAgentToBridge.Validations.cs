using Ringly.Asterisk.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService
{
    private static void ValidateConnectAgentToBridgeRequest(string bridgeId, string agentExtension)
    {
        var invalidConnectAgentToBridgeRequestException = new InvalidConnectAgentToBridgeRequestException();

        if (string.IsNullOrWhiteSpace(bridgeId))
        {
            invalidConnectAgentToBridgeRequestException.UpsertDataList(
                key: nameof(bridgeId),
                value: "Value is required");
        }

        if (string.IsNullOrWhiteSpace(agentExtension))
        {
            invalidConnectAgentToBridgeRequestException.UpsertDataList(
                key: nameof(agentExtension),
                value: "Value is required");
        }

        invalidConnectAgentToBridgeRequestException.ThrowIfContainsErrors();
    }
}
