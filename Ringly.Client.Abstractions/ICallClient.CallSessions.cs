using Ringly.Client.Abstractions.Models;

namespace Ringly.Client.Abstractions;

public partial interface ICallClient
{
    ValueTask<CallHandle> PlaceCallAsync(string targetExtension);
    ValueTask AnswerCallAsync(CallHandle handle);
    ValueTask HangupAsync(CallHandle handle);
    ValueTask MuteAsync();
    ValueTask UnmuteAsync();

    // No-op on an ICallClient with no video source registered — mirrors MuteAsync/UnmuteAsync's
    // own no-op behavior when no audio source is registered (e.g. a platform with no camera/video
    // endpoint wired up).
    ValueTask MuteVideoAsync();
    ValueTask UnmuteVideoAsync();
}
