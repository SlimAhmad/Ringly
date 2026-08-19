using Ringly.Abstractions;
using Ringly.Abstractions.Models;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi;

// The real, SQL-Server-backed ISipCredentialsStore implementation (see Part 4's storage layer),
// replacing InMemorySipCredentialsStore as this sample's DI registration — InMemorySipCredentialsStore
// stays in the codebase as a zero-setup fallback for future samples that don't need full SQL Server
// (e.g. a Blazor sample built for a quick local demo).
//
// Maps SipCredentials (the library's thin ClientId/Extension/Password contract) onto
// TelephonyIdentity (this sample's richer storage row): ClientId<->UserId,
// Extension<->SipUsername, Password<->SipCredential.
public class SqlSipCredentialsStore : ISipCredentialsStore
{
    // SipCredentials carries no notion of Rider vs Driver — that distinction only exists on
    // TelephonyIdentity, and nothing in the library-level AddClientCredentialsAsync flow this
    // store sits behind currently supplies one. Defaulting to Rider is a documented simplification,
    // not a real business rule; extending SipCredentials itself is out of scope here (Ringly-Reference
    // row #10 already fixes that signature).
    private const TelephonyIdentityType DefaultTelephonyIdentityType = TelephonyIdentityType.Rider;

    private readonly ITelephonyIdentityService telephonyIdentityService;

    public SqlSipCredentialsStore(ITelephonyIdentityService telephonyIdentityService) =>
        this.telephonyIdentityService = telephonyIdentityService;

    public async ValueTask AddAsync(SipCredentials credentials) =>
        await this.telephonyIdentityService.AddTelephonyIdentityAsync(new TelephonyIdentity
        {
            Id = Guid.NewGuid(),
            UserId = credentials.ClientId,
            SipUsername = credentials.Extension,
            SipCredential = credentials.Password,
            Type = DefaultTelephonyIdentityType,
            Status = TelephonyIdentityStatus.Active
        });

    public async ValueTask<SipCredentials?> RetrieveByClientIdAsync(Guid clientId)
    {
        TelephonyIdentity? telephonyIdentity =
            await this.telephonyIdentityService.RetrieveTelephonyIdentityByUserIdAsync(clientId);

        return telephonyIdentity is null
            ? null
            : new SipCredentials
            {
                ClientId = telephonyIdentity.UserId,
                Extension = telephonyIdentity.SipUsername,
                Password = telephonyIdentity.SipCredential
            };
    }

    public async ValueTask RemoveByClientIdAsync(Guid clientId)
    {
        TelephonyIdentity? telephonyIdentity =
            await this.telephonyIdentityService.RetrieveTelephonyIdentityByUserIdAsync(clientId);

        if (telephonyIdentity is not null)
        {
            await this.telephonyIdentityService.RemoveTelephonyIdentityByIdAsync(telephonyIdentity.Id);
        }
    }
}
