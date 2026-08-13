using System.Reactive.Linq;
using Ringly.Abstractions.Models;
using Ringly.CallCenter.Twilio.Models.Foundations.Transfers.Exceptions;

namespace Ringly.CallCenter.Twilio.Services.Foundations.Queues;

public partial class TwilioCallCenterProvider
{
    // ICallCenterProvider's transfer pair is reactive-only, modeled on Asterisk ARI's
    // ChannelTransfer event — a push signal Asterisk raises when it detects an in-progress SIP
    // REFER on a channel it controls (see AsteriskCallCenterFoundationService.Transfers.cs).
    // Twilio's Voice/TaskRouter REST APIs and event webhooks have no confirmed equivalent: Twilio
    // Voice calls don't expose raw SIP REFER signaling to the application, and no TaskRouter or
    // Voice webhook event corresponds to "a phone-initiated transfer is being requested". This is
    // a genuine platform gap, not an unresearched one — StreamTransferRequests never emits, and
    // SendTransferProgressAsync has nothing on the Twilio side to report progress to, so it only
    // validates and completes. Confirmed absent, not guessed; revisit if Twilio adds a matching
    // capability.
    public IObservable<ChannelTransferEvent> StreamTransferRequests() =>
        Observable.Empty<ChannelTransferEvent>();

    public ValueTask SendTransferProgressAsync(string channelId, TransferState state) =>
    TryCatchTransfer(() =>
    {
        ValidateChannelId(channelId);

        return ValueTask.CompletedTask;
    });

    private static void ValidateChannelId(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            var invalidTransferProgressRequestException = new InvalidTransferProgressRequestException();

            invalidTransferProgressRequestException.UpsertDataList(
                key: nameof(channelId),
                value: "Value is required");

            invalidTransferProgressRequestException.ThrowIfContainsErrors();
        }
    }
}
