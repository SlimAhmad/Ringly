using Ringly.Abstractions.Models;
using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Asterisk.Services.Orchestrations.MaskedCalls;

public interface IMaskedCallOrchestrationService
{
    ValueTask<CallSession> HandleInboundTrunkCallAsync(TrunkCallEvent trunkEvent);
}
