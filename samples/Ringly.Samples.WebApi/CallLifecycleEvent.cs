namespace Ringly.Samples.WebApi;

// Only what RideHailingCallRouter genuinely knows at each of its 3 existing decision points —
// no separate "Ringing" phase (nothing in the underlying ARI event data distinguishes it from
// Initiated in this flow), and Ended doesn't itself carry an outcome (completed vs missed) since
// that depends on whether an Answered was ever seen for this CallId, which is the consumer's
// concern, not the router's.
public enum CallLifecyclePhase
{
    Initiated,
    Answered,
    Ended
}

// CallId is the caller's own Asterisk channel ID — stable for the whole call, since it exists
// from the moment the caller's leg enters Stasis through to whichever leg ends first.
public sealed record CallLifecycleEvent(
    string CallId,
    CallLifecyclePhase Phase,
    string? CallerExtension = null,
    string? CalleeExtension = null,
    string? AsteriskBridgeId = null);

// Lets a separate service (e.g. one that persists call history) observe RideHailingCallRouter's
// call-lifecycle facts without needing to independently re-derive them from raw ARI Stasis events
// — the caller-leg-vs-callee-leg classification logic only lives correctly in one place
// (RideHailingCallRouter's own pendingCallByCalleeChannelId bookkeeping), so a second, unrelated
// subscriber to the same raw event streams would either have to duplicate that logic or risk
// getting it wrong.
public interface ICallLifecycleEventSource
{
    IObservable<CallLifecycleEvent> StreamCallLifecycleEvents();
}
