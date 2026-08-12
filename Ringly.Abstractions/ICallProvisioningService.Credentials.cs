using Ringly.Abstractions.Models;

namespace Ringly.Abstractions;

public partial interface ICallProvisioningService
{
    ValueTask<SipCredentials> AddClientCredentialsAsync(Guid clientId);
}
