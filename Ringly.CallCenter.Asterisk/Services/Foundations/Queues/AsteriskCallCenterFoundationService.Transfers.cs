using Ringly.Abstractions.Models;
using Ringly.CallCenter.Asterisk.Models.Foundations.Transfers.Exceptions;

namespace Ringly.CallCenter.Asterisk.Services.Foundations.Queues;

public partial class AsteriskCallCenterFoundationService
{
    public IObservable<ChannelTransferEvent> StreamTransferRequests() =>
        this.asteriskBroker.StreamTransferRequests();

    public ValueTask SendTransferProgressAsync(string channelId, TransferState state) =>
    TryCatchTransfer(async () =>
    {
        ValidateChannelId(channelId);

        await this.asteriskBroker.SendTransferProgressAsync(channelId, state);
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
