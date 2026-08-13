namespace Ringly.AiAgent.Abstractions.Models;

public class AiAgentConfig
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string TtsVoice { get; set; } = string.Empty;

    // e.g. "talk to a human", "agent" — matched against transcript text to trigger
    // EscalateToHumanAsync.
    public List<string> EscalationTriggerPhrases { get; set; } = [];
}
