namespace Ringly.Client.Abstractions.Models;

public class SipCredentials
{
    public Guid ClientId { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
