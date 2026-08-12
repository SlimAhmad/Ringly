using System.Reactive.Linq;
using Newtonsoft.Json.Linq;
using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Brokers;

public partial class SipTrunkBroker
{
    private const string StasisStartEventType = "StasisStart";

    // §8.3 — an inbound trunk call arrives as a StasisStart on a PJSIP channel named
    // "PJSIP/{trunkName}-{uniqueid}" (Asterisk's own PJSIP channel naming convention), landing
    // in Stasis via the §8.5 dialplan addition. Mapping is a reasonable default derived from
    // that naming convention — not acceptance-test verified against a real trunk provider
    // (would need an actual SIP trunk account to fully prove), confirm before shipping.
    public IObservable<TrunkCallEvent> StreamInboundTrunkCallsAsync() =>
        this.ariEvents
            .Where(IsInboundTrunkCall)
            .Select(MapToTrunkCallEvent);

    private static bool IsInboundTrunkCall(JObject ariEvent) =>
        ariEvent.Value<string>("type") == StasisStartEventType &&
        ariEvent["channel"]?.Value<string>("name")?.StartsWith("PJSIP/", StringComparison.Ordinal) == true;

    private static TrunkCallEvent MapToTrunkCallEvent(JObject ariEvent)
    {
        JToken channel = ariEvent["channel"]!;
        string channelName = channel.Value<string>("name") ?? string.Empty;

        return new TrunkCallEvent
        {
            ChannelId = channel.Value<string>("id") ?? string.Empty,
            TrunkName = ExtractTrunkName(channelName),
            CallerNumber = channel["caller"]?.Value<string>("number") ?? string.Empty,
            DialedNumber = channel["dialplan"]?.Value<string>("exten") ?? string.Empty
        };
    }

    private static string ExtractTrunkName(string channelName)
    {
        // "PJSIP/{trunkName}-{uniqueid}" -> {trunkName}
        string withoutTech = channelName.StartsWith("PJSIP/", StringComparison.Ordinal)
            ? channelName["PJSIP/".Length..]
            : channelName;

        int dashIndex = withoutTech.LastIndexOf('-');
        return dashIndex > 0 ? withoutTech[..dashIndex] : withoutTech;
    }
}
