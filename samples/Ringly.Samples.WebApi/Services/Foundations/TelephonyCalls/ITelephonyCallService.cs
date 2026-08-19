using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;

namespace Ringly.Samples.WebApi.Services.Foundations.TelephonyCalls;

public interface ITelephonyCallService
{
    ValueTask<TelephonyCall> AddTelephonyCallAsync(TelephonyCall telephonyCall);
    ValueTask<IQueryable<TelephonyCall>> RetrieveAllTelephonyCallsAsync();
    ValueTask<TelephonyCall> RetrieveTelephonyCallByIdAsync(Guid telephonyCallId);
    ValueTask<IQueryable<TelephonyCall>> RetrieveTelephonyCallsByCallerIdentityIdAsync(Guid callerIdentityId);
    ValueTask<TelephonyCall> ModifyTelephonyCallAsync(TelephonyCall telephonyCall);
    ValueTask<TelephonyCall> RemoveTelephonyCallByIdAsync(Guid telephonyCallId);
}
