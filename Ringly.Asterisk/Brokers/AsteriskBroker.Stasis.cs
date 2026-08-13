using System.Reactive.Linq;
using System.Text.Json;
using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string StasisStartEventType = "StasisStart";

    public IObservable<StasisStartEvent> StreamStasisStartEvents() =>
        this.ariEvents
            .Where(IsStasisStart)
            .Select(MapToStasisStartEvent);

    public async ValueTask AnswerChannelAsync(string channelId) =>
        await this.PostAsync($"{ChannelsRelativeUrl}/{Uri.EscapeDataString(channelId)}/answer");

    private static bool IsStasisStart(JsonElement ariEvent) =>
        ariEvent.TryGetProperty("type", out JsonElement type) &&
        type.GetString() == StasisStartEventType;

    private static StasisStartEvent MapToStasisStartEvent(JsonElement ariEvent) =>
        new()
        {
            ChannelId = ariEvent.GetProperty("channel").GetProperty("id").GetString() ?? string.Empty,
            Args = ariEvent.TryGetProperty("args", out JsonElement args)
                ? args.EnumerateArray().Select(arg => arg.GetString() ?? string.Empty).ToArray()
                : []
        };
}
