using System.Reactive.Linq;
using System.Text.Json;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models;

namespace Ringly.Asterisk.Brokers;

public partial class AsteriskBroker
{
    private const string ChannelTransferEventType = "ChannelTransfer";

    public IObservable<ChannelTransferEvent> StreamTransferRequests() =>
        this.ariEvents
            .Where(IsChannelTransfer)
            .Select(MapToChannelTransferEvent);

    public async ValueTask SendTransferProgressAsync(string channelId, TransferState state) =>
        await this.PostAsync(
            $"channels/{Uri.EscapeDataString(channelId)}/transfer_progress",
            new TransferProgressRequest { States = MapTransferState(state) });

    private static string MapTransferState(TransferState state) => state switch
    {
        TransferState.ChannelProgress => "channel_progress",
        TransferState.ChannelAnswered => "channel_answered",
        TransferState.ChannelUnavailable => "channel_unavailable",
        TransferState.ChannelDeclined => "channel_declined",
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    private static bool IsChannelTransfer(JsonElement ariEvent) =>
        ariEvent.TryGetProperty("type", out JsonElement type) &&
        type.GetString() == ChannelTransferEventType;

    private static ChannelTransferEvent MapToChannelTransferEvent(JsonElement ariEvent)
    {
        JsonElement referTo = ariEvent.GetProperty("refer_to");
        JsonElement referredBy = ariEvent.GetProperty("referred_by");

        JsonElement requestedDestination = referTo.GetProperty("requested_destination");
        string? protocolId = GetOptionalString(requestedDestination, "protocol_id");
        string? destination = GetOptionalString(requestedDestination, "destination");

        string sourceChannelId = GetNestedId(referredBy, "source_channel");
        string referredByBridgeId = GetNestedId(referredBy, "bridge");
        string referredByConnectedChannelId = GetNestedId(referredBy, "connected_channel");
        string heldChannelId = GetNestedId(referTo, "destination_channel");
        string referToBridgeId = GetNestedId(referTo, "bridge");
        string referToConnectedChannelId = GetNestedId(referTo, "connected_channel");

        return new ChannelTransferEvent
        {
            ChannelId = sourceChannelId,
            IsAttended = !string.IsNullOrEmpty(protocolId),
            BridgeId = referredByBridgeId,
            HeldChannelId = heldChannelId,

            ReferTo = new TransferParty
            {
                ChannelId = heldChannelId,
                BridgeId = referToBridgeId,
                ConnectedChannelId = referToConnectedChannelId,
                RequestedDestination = destination ?? protocolId ?? string.Empty
            },

            ReferredBy = new TransferParty
            {
                ChannelId = sourceChannelId,
                BridgeId = referredByBridgeId,
                ConnectedChannelId = referredByConnectedChannelId,
                RequestedDestination = string.Empty
            }
        };
    }

    private static string GetNestedId(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out JsonElement nested) &&
        nested.TryGetProperty("id", out JsonElement id)
            ? id.GetString() ?? string.Empty
            : string.Empty;

    private static string? GetOptionalString(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out JsonElement value) ? value.GetString() : null;
}
