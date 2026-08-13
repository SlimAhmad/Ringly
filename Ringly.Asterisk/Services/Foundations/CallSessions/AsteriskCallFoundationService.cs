using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;
using Ringly.Asterisk.Models;
using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;

namespace Ringly.Asterisk.Services.Foundations.CallSessions;

public partial class AsteriskCallFoundationService : ICallProvider
{
    private const string MixingBridgeType = "mixing";

    private readonly IAsteriskBroker asteriskBroker;
    private readonly ISipCredentialsStore sipCredentialsStore;
    private readonly IQueueRegistry queueRegistry;
    private readonly ILoggingBroker loggingBroker;

    public AsteriskCallFoundationService(
        IAsteriskBroker asteriskBroker,
        ISipCredentialsStore sipCredentialsStore,
        IQueueRegistry queueRegistry,
        ILoggingBroker loggingBroker)
    {
        this.asteriskBroker = asteriskBroker;
        this.sipCredentialsStore = sipCredentialsStore;
        this.queueRegistry = queueRegistry;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<CallSession> StartCallSessionAsync(CallParticipant partyA, CallParticipant partyB) =>
    TryCatch(async () =>
    {
        ValidateCallParticipant(partyA);
        ValidateCallParticipant(partyB);

        Bridge bridge = await this.asteriskBroker.InsertBridgeAsync(MixingBridgeType);
        Channel channelA = await this.asteriskBroker.InsertChannelAsync(partyA.SipExtension);
        Channel channelB = await this.asteriskBroker.InsertChannelAsync(partyB.SipExtension);

        await this.asteriskBroker.AddChannelToBridgeAsync(bridge.Id, channelA.ChannelId);
        await this.asteriskBroker.AddChannelToBridgeAsync(bridge.Id, channelB.ChannelId);

        return new CallSession
        {
            CallSessionId = Guid.NewGuid(),
            BridgeId = bridge.Id
        };
    });
}
