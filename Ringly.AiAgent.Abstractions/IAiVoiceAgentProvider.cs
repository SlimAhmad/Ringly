using Ringly.AiAgent.Abstractions.Models;

namespace Ringly.AiAgent.Abstractions;

public interface IAiVoiceAgentProvider
{
    ValueTask<AiAgentSession> StartAiSessionAsync(string channelId, AiAgentConfig config);
    ValueTask EndAiSessionAsync(Guid aiSessionId);

    // Hands off into the existing queue infrastructure (ICallCenterProvider.CreateQueueAsync) —
    // no separate escalation plumbing, per §11.
    ValueTask EscalateToHumanAsync(Guid aiSessionId, string queueName);

    IObservable<TranscriptEvent> StreamTranscriptEvents();
}
