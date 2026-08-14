using Ringly.Client.Abstractions.Models;

namespace Ringly.Client.Abstractions;

public partial interface ICallClient
{
    ValueTask<CallHandle> PlaceCallAsync(string targetExtension);
    ValueTask AnswerCallAsync(CallHandle handle);
    ValueTask HangupAsync(CallHandle handle);
    ValueTask MuteAsync();
    ValueTask UnmuteAsync();
}
