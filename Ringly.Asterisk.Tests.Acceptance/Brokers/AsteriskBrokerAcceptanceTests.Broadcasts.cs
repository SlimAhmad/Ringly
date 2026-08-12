using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Brokers;

namespace Ringly.Asterisk.Tests.Acceptance.Brokers;

// Row #22 (MAJOR ACCEPTANCE): proves the StasisBroadcast() claim race under real concurrency —
// N connected ARI applications race POST /ari/events/claim for the same broadcast channel;
// exactly one must win (204/Claimed=true), the rest must lose (409/Claimed=false). This can
// only be proven against a real Asterisk instance (docker/docker-compose.yml) since the whole
// point is Asterisk's own atomic first-claim-wins arbitration, not anything our code decides.
public partial class AsteriskBrokerAcceptanceTests
{
    [Fact]
    public async Task ShouldClaimBroadcastChannelExactlyOnceUnderConcurrencyAsync()
    {
        // given — three independent brokers, each standing in for a separate ARI application
        // instance racing to claim the same broadcast channel. The claim's "application" value
        // must be the exact name each broker subscribed to /ari/events with — Asterisk only
        // considers a claim valid from an application it currently has connected.
        string[] agentAppNames =
        [
            $"broadcast-agent-{Guid.NewGuid():N}",
            $"broadcast-agent-{Guid.NewGuid():N}",
            $"broadcast-agent-{Guid.NewGuid():N}"
        ];

        AsteriskBroker[] agentBrokers = agentAppNames
            .Select(CreateAgentBroker)
            .ToArray();

        // Give each broker's background ARI events websocket time to actually connect before
        // the broadcast fires — StasisBroadcast() only reaches applications connected *at the
        // moment it runs*, per res_stasis_broadcast.c.
        await Task.Delay(TimeSpan.FromSeconds(1));

        Task<CallBroadcastEvent>[] broadcastReceivedTasks = agentBrokers
            .Select(broker => broker.StreamCallBroadcasts().FirstAsync().ToTask())
            .ToArray();

        string? broadcastChannelId = null;

        try
        {
            // when
            await OriginateBroadcastTestChannelAsync();

            // A Local channel has two legs (;1/;2 in CLI terms, distinct ids over ARI) — the
            // originate response's id is the calling leg, not the one that actually runs the
            // dialplan and reaches StasisBroadcast(). The broadcast event itself, seen
            // identically by every connected app, is the only reliable source for the real id.
            CallBroadcastEvent[] receivedEvents =
                await Task.WhenAll(broadcastReceivedTasks).WaitAsync(TimeSpan.FromSeconds(10));

            broadcastChannelId = receivedEvents[0].ChannelId;

            foreach (CallBroadcastEvent receivedEvent in receivedEvents)
            {
                receivedEvent.ChannelId.Should().Be(broadcastChannelId);
            }

            ValueTask<ClaimResult>[] claimTasks =
            [
                agentBrokers[0].ClaimCallAsync(broadcastChannelId, agentAppNames[0]),
                agentBrokers[1].ClaimCallAsync(broadcastChannelId, agentAppNames[1]),
                agentBrokers[2].ClaimCallAsync(broadcastChannelId, agentAppNames[2])
            ];

            ClaimResult[] results = await Task.WhenAll(
                claimTasks[0].AsTask(),
                claimTasks[1].AsTask(),
                claimTasks[2].AsTask());

            // then
            results.Count(result => result.Claimed).Should().Be(1);
            results.Count(result => !result.Claimed).Should().Be(2);
            results.Should().OnlyContain(result => result.ChannelId == broadcastChannelId);
        }
        finally
        {
            if (broadcastChannelId is not null)
            {
                await this.rawAriClient.DeleteAsync($"ari/channels/{broadcastChannelId}");
            }
        }
    }

    private static AsteriskBroker CreateAgentBroker(string appName)
    {
        var options = Options.Create(new AsteriskOptions
        {
            BaseUrl = BaseUrl,
            Username = AriUsername,
            Password = AriPassword,
            StasisAppName = appName,
            DialplanContext = "ride_hailing",
            UseWebRtcTransport = true,
            AmiPort = 5038,
            AmiUsername = "ringly",
            AmiSecret = "ringly-dev-ami"
        });

        return new AsteriskBroker(options);
    }

    private async Task OriginateBroadcastTestChannelAsync()
    {
        var content = new StringContent(
            content: string.Empty,
            encoding: System.Text.Encoding.UTF8,
            mediaType: "application/json");

        string relativeUrl =
            "ari/channels?endpoint=" + Uri.EscapeDataString("Local/9999@broadcast_test") +
            "&extension=9999&context=broadcast_test&priority=1";

        using HttpResponseMessage response = await this.rawAriClient.PostAsync(relativeUrl, content);
        response.EnsureSuccessStatusCode();
    }
}
