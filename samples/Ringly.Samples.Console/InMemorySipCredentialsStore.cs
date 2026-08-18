using System.Collections.Concurrent;
using Ringly.Abstractions;
using Ringly.Abstractions.Models;

namespace Ringly.Samples.Console;

// Ringly needs somewhere to look up a client's provisioned SIP credentials, but doesn't own
// storage for them (see docs/call-provider.md). For Twilio, "Extension" holds a phone number
// rather than a SIP extension — TwilioCallProvider.RouteToQueueAsync dials it as the "To" number.
public class InMemorySipCredentialsStore : ISipCredentialsStore
{
    private readonly ConcurrentDictionary<Guid, SipCredentials> credentials = new();

    public ValueTask AddAsync(SipCredentials credentials)
    {
        this.credentials[credentials.ClientId] = credentials;
        return ValueTask.CompletedTask;
    }

    public ValueTask<SipCredentials?> RetrieveByClientIdAsync(Guid clientId) =>
        ValueTask.FromResult(this.credentials.GetValueOrDefault(clientId));

    public ValueTask RemoveByClientIdAsync(Guid clientId)
    {
        this.credentials.TryRemove(clientId, out _);
        return ValueTask.CompletedTask;
    }
}
