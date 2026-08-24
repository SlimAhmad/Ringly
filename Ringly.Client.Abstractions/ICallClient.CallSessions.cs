using Ringly.Client.Abstractions.Models;

namespace Ringly.Client.Abstractions;

public partial interface ICallClient
{
    // includeVideo controls whether THIS side offers a video track at all — lets a caller
    // deliberately place an audio-only call even when a video source/sink is registered, useful
    // for isolating whether audio behaves differently with video's extra bandwidth/processing
    // sharing the same call. No effect on a client with no video source/sink registered in the
    // first place (there was never a video track to offer either way).
    ValueTask<CallHandle> PlaceCallAsync(string targetExtension, bool includeVideo = true);
    ValueTask AnswerCallAsync(CallHandle handle);
    ValueTask HangupAsync(CallHandle handle);

    // Sends a real SIP REFER to the current call's own dialog peer, asking IT (not this client)
    // to place a new call to targetExtension and replace this one — the standard blind-transfer
    // mechanism, distinct from HangupAsync (which just ends this side's own participation).
    // Returns whether the far end accepted the transfer request within timeoutSeconds.
    ValueTask<bool> BlindTransferAsync(CallHandle handle, string targetExtension, int timeoutSeconds = 10);
    ValueTask MuteAsync();
    ValueTask UnmuteAsync();

    // No-op on an ICallClient with no video source registered — mirrors MuteAsync/UnmuteAsync's
    // own no-op behavior when no audio source is registered (e.g. a platform with no camera/video
    // endpoint wired up).
    ValueTask MuteVideoAsync();
    ValueTask UnmuteVideoAsync();
}
