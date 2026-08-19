using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyIdentities;

public interface ITelephonyIdentityService
{
    ValueTask<TelephonyIdentity> AddTelephonyIdentityAsync(TelephonyIdentity telephonyIdentity);
    ValueTask<IQueryable<TelephonyIdentity>> RetrieveAllTelephonyIdentitiesAsync();
    ValueTask<TelephonyIdentity> RetrieveTelephonyIdentityByIdAsync(Guid telephonyIdentityId);
    ValueTask<TelephonyIdentity?> RetrieveTelephonyIdentityByUserIdAsync(Guid userId);
    ValueTask<TelephonyIdentity> ModifyTelephonyIdentityAsync(TelephonyIdentity telephonyIdentity);
    ValueTask<TelephonyIdentity> RemoveTelephonyIdentityByIdAsync(Guid telephonyIdentityId);
}
