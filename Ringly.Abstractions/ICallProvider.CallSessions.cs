using Ringly.Abstractions.Models;

namespace Ringly.Abstractions;

public partial interface ICallProvider
{
    ValueTask<CallSession> StartCallSessionAsync(CallParticipant partyA, CallParticipant partyB);

    // Cold support entry point (§3.1a) — customer taps "contact support" with no active call in
    // progress. NOT for escalating an already-connected call — see EscalateToQueueAsync below
    // for that.
    ValueTask<CallSession> RouteToQueueAsync(Guid customerId, string queueName);

    // Hands a call Ringly never originated or tracked (e.g. one an external AI agent like
    // Dograh is currently handling on its own) into one of the same queues RouteToQueueAsync
    // uses for a cold entry — keyed by the call's own provider-native channel id rather than a
    // Ringly customerId, since Ringly has no customer record for a call it didn't originate.
    ValueTask<CallSession> EscalateToQueueAsync(string channelId, string queueName);

    // Same escalation as EscalateToQueueAsync, but keyed by the caller's own phone number rather
    // than the provider-native channel id — for an external AI agent whose tool-calling mechanism
    // never exposes the live channel id to the tool itself (confirmed against Dograh's own
    // support: "the telephony channel ID isn't sent" to a tool call; only caller_number/
    // called_number are automatically available). Implementations resolve the live call
    // themselves from whatever identifies an in-progress call by caller number in their own
    // provider (e.g. Asterisk ARI's channel list, Twilio's in-progress Calls list).
    ValueTask<CallSession> EscalateToQueueByCallerNumberAsync(string callerNumber, string queueName);

    // Bridges a claiming agent's own channel into an already-held customer's bridge/conference
    // (bridgeId is CallSession.BridgeId, customerChannelId is CallSession.CustomerChannelId, both
    // from the RouteToQueueAsync call that put the customer there) — the counterpart that
    // actually connects the two sides once an agent claims a waiting customer, rather than
    // leaving them on hold indefinitely. Returns AgentConnection rather than a bare Channel: the
    // bridge actually carrying audio afterward isn't always the same one passed in (Asterisk's
    // implementation creates a fresh mixing bridge internally), so callers need the real one back
    // — confirmed live as a bug when it wasn't: callers kept reporting the original holding
    // bridge, which downstream consumers (e.g. call recording) then pointed at an empty bridge.
    ValueTask<AgentConnection> ConnectAgentToQueueAsync(string bridgeId, string customerChannelId, string agentExtension);

    // For a call already sitting in a bridge Ringly doesn't own and never created (e.g. one an
    // external AI agent like Dograh's own Call Transfer tool bridged a caller into) — adds the
    // claiming agent's channel directly into that existing bridge, with no removal/recreation
    // step. Confirmed live as a hard requirement for Dograh specifically: any attempt to move or
    // remove a channel FROM a bridge Dograh's own ARI app still considers itself responsible for
    // makes Dograh's app conclude the call ended and tear down its own side defensively — adding
    // a third participant to the bridge it already owns does not trigger that reaction the same
    // way. Unlike ConnectAgentToQueueAsync, there is no "customer channel" to remove from a
    // holding bridge here — the caller and whatever Dograh already bridged them with (e.g.
    // Ringly's own always-answering registrar endpoint) both stay exactly where they are.
    ValueTask<AgentConnection> ConnectAgentToBridgeAsync(string bridgeId, string agentExtension);
}
