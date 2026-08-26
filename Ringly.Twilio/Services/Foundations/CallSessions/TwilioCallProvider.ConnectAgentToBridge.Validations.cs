using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

public partial class TwilioCallProvider
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
