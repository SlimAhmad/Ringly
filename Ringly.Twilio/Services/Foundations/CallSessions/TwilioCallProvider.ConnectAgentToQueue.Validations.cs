using Ringly.Twilio.Models.Foundations.CallSessions.Exceptions;

namespace Ringly.Twilio.Services.Foundations.CallSessions;

public partial class TwilioCallProvider
{
    private static void ValidateConnectAgentToQueueRequest(string bridgeId, string agentExtension)
    {
        var invalidConnectAgentToQueueRequestException = new InvalidConnectAgentToQueueRequestException();

        if (string.IsNullOrWhiteSpace(bridgeId))
        {
            invalidConnectAgentToQueueRequestException.UpsertDataList(
                key: nameof(bridgeId),
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
