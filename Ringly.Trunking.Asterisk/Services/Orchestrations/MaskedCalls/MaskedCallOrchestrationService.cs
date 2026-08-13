using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Trunking.Abstractions;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Brokers;
using Ringly.Trunking.Asterisk.Models.Orchestrations.MaskedCalls.Exceptions;

namespace Ringly.Trunking.Asterisk.Services.Orchestrations.MaskedCalls;

public partial class MaskedCallOrchestrationService : IMaskedCallOrchestrationService
{
    private readonly IMaskingSessionStore maskingSessionStore;
    private readonly ICallProvider callProvider;
    private readonly ILoggingBroker loggingBroker;

    public MaskedCallOrchestrationService(
        IMaskingSessionStore maskingSessionStore,
        ICallProvider callProvider,
        ILoggingBroker loggingBroker)
    {
        this.maskingSessionStore = maskingSessionStore;
        this.callProvider = callProvider;
        this.loggingBroker = loggingBroker;
    }

    // §8.3 — the trunk only knows "call arrived for masked number X"; this maps
    // DialedNumber -> active session -> the other party's extension.
    public ValueTask<CallSession> HandleInboundTrunkCallAsync(TrunkCallEvent trunkEvent) =>
    TryCatch(async () =>
    {
        ValidateTrunkCallEvent(trunkEvent);

        MaskingSession? session =
            await this.maskingSessionStore.RetrieveByMaskedNumberAsync(trunkEvent.DialedNumber);

        if (session is null || session.IsExpired)
        {
            throw new MaskingSessionNotFoundException(trunkEvent.DialedNumber);
        }

        return await this.callProvider.StartCallSessionAsync(
            new CallParticipant { SipExtension = session.OtherPartyExtension },
            new CallParticipant { SipExtension = trunkEvent.ChannelId });
    });
}
