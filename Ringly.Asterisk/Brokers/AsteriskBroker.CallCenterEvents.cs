using System.Reactive.Linq;
using System.Text.Json;
using Ringly.Abstractions.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string AriEventSource = "ARI";
    private const string AmiEventSource = "AMI";

    public IObservable<CallCenterEvent> StreamCallCenterEvents() =>
        Observable.Merge(
            this.ariEvents.Select(MapAriEventToCallCenterEvent),
            this.amiEvents.Where(HasEventKey).Select(MapAmiEventToCallCenterEvent));

    private static bool HasEventKey(IReadOnlyDictionary<string, string> amiEvent) =>
        amiEvent.ContainsKey("Event");

    private static CallCenterEvent MapAriEventToCallCenterEvent(JsonElement ariEvent) =>
        new()
        {
            EventType = ariEvent.TryGetProperty("type", out JsonElement type)
                ? type.GetString() ?? string.Empty
                : string.Empty,

            Source = AriEventSource,

            OccurredDate = ariEvent.TryGetProperty("timestamp", out JsonElement timestamp) &&
                DateTimeOffset.TryParse(timestamp.GetString(), out DateTimeOffset parsedTimestamp)
                    ? parsedTimestamp
                    : DateTimeOffset.UtcNow
        };

    private static CallCenterEvent MapAmiEventToCallCenterEvent(IReadOnlyDictionary<string, string> amiEvent) =>
        new()
        {
            EventType = amiEvent.GetValueOrDefault("Event", string.Empty),
            Source = AmiEventSource,
            OccurredDate = DateTimeOffset.UtcNow
        };
}
