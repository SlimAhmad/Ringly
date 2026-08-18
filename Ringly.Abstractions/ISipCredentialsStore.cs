using Ringly.Abstractions.Models;

namespace Ringly.Abstractions;

// App-level persistence contract — Ringly needs to look up a client's provisioned SIP
// credentials (row #10's AddClientCredentialsAsync output) to route a cold support request, but
// doesn't own where that gets stored; no concrete implementation ships from this library.
public interface ISipCredentialsStore
{
    // Nullable, not throwing — matches this codebase's established not-found convention
    // (e.g. IMaskingSessionStore, row #26): the store reports absence, the calling foundation
    // service decides what that means and throws its own domain exception.
    ValueTask<SipCredentials?> RetrieveByClientIdAsync(Guid clientId);

    ValueTask AddAsync(SipCredentials credentials);
    ValueTask RemoveByClientIdAsync(Guid clientId);
}
