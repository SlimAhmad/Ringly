using Ringly.Abstractions;
using Ringly.Abstractions.Models;

namespace Ringly.Samples.WebApi;

// Combines the library's ICallProvisioningService (provision/deprovision against Asterisk) with
// this sample's own ISipCredentialsStore (persist the result) — this pairing is app-specific glue,
// not library behavior, so it lives here rather than in Ringly.Asterisk. Exists so ClientsController
// has a single service dependency instead of two, per the controller "one service dependency" rule.
public class ClientCredentialsService
{
    private readonly ICallProvisioningService provisioningService;
    private readonly ISipCredentialsStore credentialsStore;

    public ClientCredentialsService(
        ICallProvisioningService provisioningService,
        ISipCredentialsStore credentialsStore)
    {
        this.provisioningService = provisioningService;
        this.credentialsStore = credentialsStore;
    }

    public async ValueTask<SipCredentials> AddAsync(Guid clientId)
    {
        SipCredentials credentials = await this.provisioningService.AddClientCredentialsAsync(clientId);
        await this.credentialsStore.AddAsync(credentials);

        return credentials;
    }

    public ValueTask<SipCredentials?> RetrieveByClientIdAsync(Guid clientId) =>
        this.credentialsStore.RetrieveByClientIdAsync(clientId);

    public async ValueTask<bool> RemoveByClientIdAsync(Guid clientId)
    {
        SipCredentials? credentials = await this.credentialsStore.RetrieveByClientIdAsync(clientId);

        if (credentials is null)
        {
            return false;
        }

        await this.provisioningService.RemoveClientCredentialsAsync(credentials.Extension);
        await this.credentialsStore.RemoveByClientIdAsync(clientId);

        return true;
    }
}
