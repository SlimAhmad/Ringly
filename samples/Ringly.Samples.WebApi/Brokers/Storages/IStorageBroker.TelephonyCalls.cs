using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;

namespace Ringly.Samples.WebApi.Brokers.Storages;

public partial interface IStorageBroker
{
    ValueTask<TelephonyCall> InsertTelephonyCallAsync(TelephonyCall telephonyCall);
    ValueTask<IQueryable<TelephonyCall>> SelectAllTelephonyCallsAsync();
    ValueTask<TelephonyCall> SelectTelephonyCallByIdAsync(Guid telephonyCallId);
    ValueTask<IQueryable<TelephonyCall>> SelectTelephonyCallsByCallerIdentityIdAsync(Guid callerIdentityId);
    ValueTask<TelephonyCall> UpdateTelephonyCallAsync(TelephonyCall telephonyCall);
    ValueTask<TelephonyCall> DeleteTelephonyCallAsync(TelephonyCall telephonyCall);
}
