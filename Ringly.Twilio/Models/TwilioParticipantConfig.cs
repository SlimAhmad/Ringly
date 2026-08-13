namespace Ringly.Twilio.Models;

public class TwilioParticipantConfig
{
    public string To { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public bool Muted { get; set; } = false;
    public bool Hold { get; set; } = false;
    public bool Coaching { get; set; } = false;

    // Required by Twilio when Coaching = true — the CallSid of the participant being coached.
    public string? CallSidToCoach { get; set; }

    public bool EarlyMedia { get; set; } = true;
    public bool StartConferenceOnEnter { get; set; } = true;
    public bool EndConferenceOnExit { get; set; } = false;
}
