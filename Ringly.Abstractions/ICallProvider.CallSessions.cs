using Ringly.Abstractions.Models;

namespace Ringly.Abstractions;

public partial interface ICallProvider
{
    ValueTask<CallSession> StartCallSessionAsync(CallParticipant partyA, CallParticipant partyB);
}
