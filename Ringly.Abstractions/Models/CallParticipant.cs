namespace Ringly.Abstractions.Models;

public class CallParticipant
{
    public Guid Id { get; set; }
    public string SipExtension { get; set; } = string.Empty;
}
