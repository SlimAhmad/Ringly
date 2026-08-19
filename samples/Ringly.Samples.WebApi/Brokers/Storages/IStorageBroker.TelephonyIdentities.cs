using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Brokers.Storages;

public partial interface IStorageBroker
{
    ValueTask<TelephonyIdentity> InsertTelephonyIdentityAsync(TelephonyIdentity telephonyIdentity);
    ValueTask<IQueryable<TelephonyIdentity>> SelectAllTelephonyIdentitiesAsync();
    ValueTask<TelephonyIdentity> SelectTelephonyIdentityByIdAsync(Guid telephonyIdentityId);
    ValueTask<TelephonyIdentity?> SelectTelephonyIdentityByUserIdAsync(Guid userId);
    ValueTask<TelephonyIdentity?> SelectTelephonyIdentityBySipUsernameAsync(string sipUsername);
    ValueTask<TelephonyIdentity> UpdateTelephonyIdentityAsync(TelephonyIdentity telephonyIdentity);
    ValueTask<TelephonyIdentity> DeleteTelephonyIdentityAsync(TelephonyIdentity telephonyIdentity);
}
