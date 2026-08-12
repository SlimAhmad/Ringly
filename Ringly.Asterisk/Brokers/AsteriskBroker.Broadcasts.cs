using System.Reactive.Linq;
using System.Text.Json;
using Ringly.Abstractions.Models;
using RESTFulSense.Exceptions;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string CallBroadcastEventType = "CallBroadcast";
    private const string ClaimRelativeUrl = "events/claim";
    private const string DeviceStatesRelativeUrlFormat = "deviceStates/{0}";
    private const string NotInUseDeviceState = "NOT_INUSE";
    private const string UnavailableDeviceState = "UNAVAILABLE";

    public IObservable<CallBroadcastEvent> StreamCallBroadcasts() =>
        this.ariEvents
            .Where(IsCallBroadcast)
            .Select(MapToCallBroadcastEvent);

    public async ValueTask<ClaimResult> ClaimCallAsync(string channelId, string agentAppName)
    {
        string relativeUrl =
            $"{ClaimRelativeUrl}?channelId={Uri.EscapeDataString(channelId)}&application={Uri.EscapeDataString(agentAppName)}";

        try
        {
            await this.PostAsync(relativeUrl);
            return new ClaimResult { Claimed = true, ChannelId = channelId };
        }
        catch (HttpResponseConflictException)
        {
            return new ClaimResult { Claimed = false, ChannelId = channelId };
        }
    }

    public async ValueTask SetAgentAvailabilityAsync(string agentAppName, bool isAvailable)
    {
        string relativeUrl = string.Format(DeviceStatesRelativeUrlFormat, AgentDeviceName(agentAppName));
        string deviceState = isAvailable ? NotInUseDeviceState : UnavailableDeviceState;

        await this.PutAsync($"{relativeUrl}?deviceState={Uri.EscapeDataString(deviceState)}");
    }

    private static string AgentDeviceName(string agentAppName) =>
        Uri.EscapeDataString($"Stasis:Agent-{agentAppName}");

    private static bool IsCallBroadcast(JsonElement ariEvent) =>
        ariEvent.TryGetProperty("type", out JsonElement type) &&
        type.GetString() == CallBroadcastEventType;

    private static CallBroadcastEvent MapToCallBroadcastEvent(JsonElement ariEvent)
    {
        JsonElement channel = ariEvent.GetProperty("channel");

        return new CallBroadcastEvent
        {
            ChannelId = channel.GetProperty("id").GetString() ?? string.Empty,

            CallerNumber = ariEvent.TryGetProperty("caller", out JsonElement caller)
                ? caller.GetString() ?? string.Empty
                : string.Empty,

            CalledExtension = ariEvent.TryGetProperty("called", out JsonElement called)
                ? called.GetString() ?? string.Empty
                : string.Empty,

            ChannelVars = channel.TryGetProperty("channelvars", out JsonElement channelVars)
                ? channelVars.EnumerateObject().ToDictionary(
                    property => property.Name,
                    property => property.Value.GetString() ?? string.Empty)
                : []
        };
    }
}
