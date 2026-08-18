using System.Collections.Concurrent;
using Ringly.Abstractions;
using Ringly.Abstractions.Models;

namespace Ringly.Samples.WebApi;

// Ringly needs somewhere to look up a client's provisioned SIP credentials, but doesn't own
// storage for them (see docs/call-provider.md) — a real app would back this with a database.
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
