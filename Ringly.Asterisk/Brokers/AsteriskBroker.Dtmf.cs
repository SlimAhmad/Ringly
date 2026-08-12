using System.Reactive.Linq;
using System.Text.Json;
using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string ChannelDtmfReceivedEventType = "ChannelDtmfReceived";

    public IObservable<DtmfEvent> StreamDtmfEvents() =>
        this.ariEvents
            .Where(IsChannelDtmfReceived)
            .Select(MapToDtmfEvent);

    private static bool IsChannelDtmfReceived(JsonElement ariEvent) =>
        ariEvent.TryGetProperty("type", out JsonElement type) &&
        type.GetString() == ChannelDtmfReceivedEventType;

    private static DtmfEvent MapToDtmfEvent(JsonElement ariEvent) =>
        new()
        {
            ChannelId = ariEvent.GetProperty("channel").GetProperty("id").GetString() ?? string.Empty,
            Digit = ariEvent.GetProperty("digit").GetString() ?? string.Empty,
            DurationMs = ariEvent.TryGetProperty("duration_ms", out JsonElement duration)
                ? duration.GetInt32()
                : 0
        };
}
