using System.Reactive.Linq;
using System.Text.Json;
using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string StasisStartEventType = "StasisStart";
    private const string ChannelStateChangeEventType = "ChannelStateChange";

    public IObservable<StasisStartEvent> StreamStasisStartEvents() =>
        this.ariEvents
            .Where(ariEvent => IsEventType(ariEvent, StasisStartEventType))
            .Select(MapToStasisStartEvent);

    public IObservable<ChannelStateChangeEvent> StreamChannelStateChangeEvents() =>
        this.ariEvents
            .Where(ariEvent => IsEventType(ariEvent, ChannelStateChangeEventType))
            .Select(MapToChannelStateChangeEvent);

    public async ValueTask AnswerChannelAsync(string channelId) =>
        await this.PostAsync($"{ChannelsRelativeUrl}/{Uri.EscapeDataString(channelId)}/answer");

    public async ValueTask RingChannelAsync(string channelId) =>
        await this.PostAsync($"{ChannelsRelativeUrl}/{Uri.EscapeDataString(channelId)}/ring");

    private static bool IsEventType(JsonElement ariEvent, string eventType) =>
        ariEvent.TryGetProperty("type", out JsonElement type) &&
        type.GetString() == eventType;

    private static StasisStartEvent MapToStasisStartEvent(JsonElement ariEvent) =>
        new()
        {
            ChannelId = ariEvent.GetProperty("channel").GetProperty("id").GetString() ?? string.Empty,
            Args = ariEvent.TryGetProperty("args", out JsonElement args)
                ? args.EnumerateArray().Select(arg => arg.GetString() ?? string.Empty).ToArray()
                : []
        };

    private static ChannelStateChangeEvent MapToChannelStateChangeEvent(JsonElement ariEvent) =>
        new()
        {
            ChannelId = ariEvent.GetProperty("channel").GetProperty("id").GetString() ?? string.Empty,
            State = ariEvent.GetProperty("channel").GetProperty("state").GetString() ?? string.Empty
        };
}
