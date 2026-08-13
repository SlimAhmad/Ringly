using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
    private static readonly TimeSpan StasisEntryTimeout = TimeSpan.FromSeconds(30);

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
        Channel channelA = await this.asteriskBroker.InsertChannelAsync($"PJSIP/{partyA.SipExtension}");
        Channel channelB = await this.asteriskBroker.InsertChannelAsync($"PJSIP/{partyB.SipExtension}");

        // A just-originated channel isn't biddable yet — ARI rejects bridges/{id}/addChannel with
        // "Channel not in Stasis application" (confirmed against a real Asterisk instance) until
        // the channel actually answers and Asterisk routes it into the Stasis app, which only
        // happens once the callee picks up. Waiting for each channel's own StasisStart before
        // bridging is what actually makes that transition observable.
        await WaitForStasisStartAsync(this.asteriskBroker, channelA.ChannelId);
        await WaitForStasisStartAsync(this.asteriskBroker, channelB.ChannelId);

        await this.asteriskBroker.AddChannelToBridgeAsync(bridge.Id, channelA.ChannelId);
        await this.asteriskBroker.AddChannelToBridgeAsync(bridge.Id, channelB.ChannelId);

        return new CallSession
        {
            CallSessionId = Guid.NewGuid(),
            BridgeId = bridge.Id
        };
    });

    private static Task WaitForStasisStartAsync(IAsteriskBroker asteriskBroker, string channelId) =>
        asteriskBroker.StreamStasisStartEvents()
            .Where(stasisStartEvent => stasisStartEvent.ChannelId == channelId)
            .Take(1)
            .Timeout(StasisEntryTimeout)
            .ToTask();
}
