using System.Net;
using System.Reactive.Linq;
using System.Text.Json;
using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string StasisStartEventType = "StasisStart";
    private const string ClaimRelativeUrl = "events/claim";
    private const string AsteriskVariableRelativeUrl = "asterisk/variable";

    public IObservable<CallBroadcastEvent> StreamCallBroadcasts() =>
        this.ariEvents
            .Where(IsStasisStart)
            .Select(MapToCallBroadcastEvent);

    public async ValueTask<ClaimResult> ClaimCallAsync(string channelId, string agentAppName)
    {
        HttpResponseMessage response = await this.ariClient.PostAsync(
            $"{ClaimRelativeUrl}?channelId={Uri.EscapeDataString(channelId)}&app={Uri.EscapeDataString(agentAppName)}",
            content: null);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return new ClaimResult { Claimed = false, ChannelId = channelId };
        }

        response.EnsureSuccessStatusCode();

        return new ClaimResult { Claimed = true, ChannelId = channelId };
    }

    public async ValueTask SetAgentAvailabilityAsync(string agentAppName, bool isAvailable) =>
        await this.PostAsync(
            $"{AsteriskVariableRelativeUrl}?variable={Uri.EscapeDataString(AgentAvailabilityVariableName(agentAppName))}" +
            $"&value={(isAvailable ? "true" : "false")}");

    private static string AgentAvailabilityVariableName(string agentAppName) =>
        $"AGENT_AVAILABLE_{agentAppName}";

    private static bool IsStasisStart(JsonElement ariEvent) =>
        ariEvent.TryGetProperty("type", out JsonElement type) &&
        type.GetString() == StasisStartEventType;

    private static CallBroadcastEvent MapToCallBroadcastEvent(JsonElement ariEvent)
    {
        JsonElement channel = ariEvent.GetProperty("channel");

        return new CallBroadcastEvent
        {
            ChannelId = channel.GetProperty("id").GetString() ?? string.Empty,

            CallerNumber = channel.TryGetProperty("caller", out JsonElement caller) &&
                caller.TryGetProperty("number", out JsonElement number)
                    ? number.GetString() ?? string.Empty
                    : string.Empty,

            CalledExtension = channel.TryGetProperty("dialplan", out JsonElement dialplan) &&
                dialplan.TryGetProperty("exten", out JsonElement exten)
                    ? exten.GetString() ?? string.Empty
                    : string.Empty,

            ChannelVars = channel.TryGetProperty("channelvars", out JsonElement channelVars)
                ? channelVars.EnumerateObject().ToDictionary(
                    property => property.Name,
                    property => property.Value.GetString() ?? string.Empty)
                : []
        };
    }
}
